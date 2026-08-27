using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Application.Deployment;
using JayTom.Dws.Application.UseCases;
using JayTom.Dws.Client.Service.DefaultConfiguration;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.Service.Sorting;
using NLog;
using JayTom.Dws.Abstractions.Observability;
using JayTom.Dws.Client.Observability;

namespace JayTom.Dws.Client.Service.Runtime;

/// <summary>实现桌面应用启动、健康输出和有界停机编排。</summary>
public sealed class ApplicationLifecycleCoordinator : IApplicationLifecycleCoordinator
{
    /// <summary>单个设备或分拣组件在停机阶段允许占用的最长时间。</summary>
    private static readonly TimeSpan ComponentStopTimeout = TimeSpan.FromSeconds(5);
    /// <summary>默认配置初始化服务。</summary>
    private readonly IDefaultConfigurationService _defaultConfigurationService;
    /// <summary>应用配置统一存取边界。</summary>
    private readonly ISettingsStore _settingsStore;
    /// <summary>可回滚的配置版本迁移运行器。</summary>
    private readonly IApplicationCommandHandler<
        MigrateConfigurationCommand,
        ConfigurationMigrationReceipt> _configurationMigration;
    /// <summary>原生依赖启动前完整性验证器。</summary>
    private readonly NativeDependencyValidator _nativeDependencyValidator;
    /// <summary>受管后台工作流监督器。</summary>
    private readonly IHostedServiceSupervisor _hostedServiceSupervisor;
    /// <summary>设备生产者生命周期服务。</summary>
    private readonly IDeviceService _deviceService;
    /// <summary>分拣生产者生命周期服务。</summary>
    private readonly ISortingService _sortingService;

    /// <summary>创建应用生命周期协调器。</summary>
    public ApplicationLifecycleCoordinator(
        IDefaultConfigurationService defaultConfigurationService,
        ISettingsStore settingsStore,
        IApplicationCommandHandler<MigrateConfigurationCommand, ConfigurationMigrationReceipt> configurationMigration,
        NativeDependencyValidator nativeDependencyValidator,
        IHostedServiceSupervisor hostedServiceSupervisor,
        IDeviceService deviceService,
        ISortingService sortingService)
    {
        _defaultConfigurationService = defaultConfigurationService;
        _settingsStore = settingsStore;
        _configurationMigration = configurationMigration;
        _nativeDependencyValidator = nativeDependencyValidator;
        _hostedServiceSupervisor = hostedServiceSupervisor;
        _deviceService = deviceService;
        _sortingService = sortingService;
    }

    /// <summary>初始化配置并按依赖顺序启动后台工作流。</summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using CorrelationScope correlation = CorrelationContext.Begin();
        using Activity? activity = DwsDiagnostics.StartActivity("application.start");
        long started = Stopwatch.GetTimestamp();
        var nativeValidation = await _nativeDependencyValidator.ValidateAsync(
            AppContext.BaseDirectory,
            Path.Combine(AppContext.BaseDirectory, "native-assets.win-x64.json"),
            cancellationToken).ConfigureAwait(false);
        if (!nativeValidation.IsSuccess)
        {
            throw new InvalidOperationException(
                $"原生依赖验证失败 [{nativeValidation.ErrorCode}]：{nativeValidation.ErrorMessage}");
        }

        await _defaultConfigurationService.WriteDefaultConfiguration().ConfigureAwait(false);
        var migration = await _configurationMigration
            .HandleAsync(
                new MigrateConfigurationCommand(ConfigurationMigrationRunner.CurrentSchemaVersion),
                cancellationToken)
            .ConfigureAwait(false);
        if (!migration.IsSuccess)
        {
            throw new InvalidOperationException(
                $"配置迁移失败 [{migration.ErrorCode}]：{migration.ErrorMessage}");
        }
        string? language = await _settingsStore
            .GetRawAsync("Language", cancellationToken)
            .ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(language))
        {
            var culture = new CultureInfo(language);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        await _hostedServiceSupervisor.StartAsync(cancellationToken).ConfigureAwait(false);
        foreach (var serviceState in _hostedServiceSupervisor.GetHealthSnapshot())
        {
            LogManager.GetCurrentClassLogger()
                .Info("后台服务 {ServiceName} 当前状态为 {ServiceState}", serviceState.Key, serviceState.Value);
        }
        activity?.SetStatus(ActivityStatusCode.Ok);
        DwsDiagnostics.RecordOperation(
            "application.start",
            true,
            Stopwatch.GetElapsedTime(started));
        LogManager.GetCurrentClassLogger().InfoOperation("application.start", "completed");
    }

    /// <summary>按设备、分拣、后台消费者的依赖逆序执行停机。</summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await StopComponentAsync(
            "设备服务",
            () => _deviceService.RunningStatus,
            () => _deviceService.Stop(),
            cancellationToken).ConfigureAwait(false);
        await StopComponentAsync(
            "分拣服务",
            () => _sortingService.RunningStatus,
            () => _sortingService.Stop(),
            cancellationToken).ConfigureAwait(false);

        // 先停止设备和分拣生产者，再停止并刷新后台消费者，避免停机窗口继续产生新工作。
        await _hostedServiceSupervisor.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>在有界时间内停止单个生产组件，失败时继续后续停机步骤。</summary>
    private static async Task StopComponentAsync(
        string componentName,
        Func<bool> isRunning,
        Func<Task> stop,
        CancellationToken cancellationToken)
    {
        if (!isRunning())
        {
            return;
        }

        try
        {
            await stop()
                .WaitAsync(ComponentStopTimeout, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogManager.GetCurrentClassLogger()
                .Error(exception, "停止 {ComponentName} 失败或超时，继续执行其余停机步骤", componentName);
        }
    }
}
