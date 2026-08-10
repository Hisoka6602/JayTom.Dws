using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Domain.Repository.LocalConf.IpcNvrConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Repository.LocalLog;
using JayTom.Dws.Infrastructure;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.IpcNvrConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig.ConnectionParams;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.Infrastructure.Repository.LocalLog;
using JayTom.Dws.Infrastructure.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace JayTom.Dws.Client.Composition;

/// <summary>集中注册本地数据库上下文与仓储适配器。</summary>
internal static class PersistenceRegistration {
    /// <summary>注册持久化基础设施。</summary>
    public static IServiceCollection AddDwsPersistence(this IServiceCollection services) {
        services.AddPooledDbContextFactory<SqliteContext>(
            options => ConfigureSqlite(options, "Data.db"), 32);
        services.AddPooledDbContextFactory<SqliteConfContext>(
            options => ConfigureSqlite(options, "Configuration.db"), 32);
        services.AddPooledDbContextFactory<SqliteLogsContext>(
            options => ConfigureSqlite(options, "ClientLogs.db"), 32);

        services.AddTransient<IPackageRepository, PackageRepository>();
        services.AddTransient<IBarCodeRepository, BarCodeRepository>();
        services.AddTransient<ISoundRepository, SoundRepository>();
        services.AddTransient<IVolumeRepository, VolumeRepository>();
        services.AddTransient<IWeightRepository, WeightRepository>();
        services.AddTransient<IUploadRepository, UploadRepository>();
        services.AddTransient<ISortingRepository, SortingRepository>();
        services.AddTransient<IOcrRepository, OcrRepository>();
        services.AddTransient<IImageRepository, ImageRepository>();
        services.AddTransient<ICloudVideoUploadRepository, CloudVideoUploadRepository>();
        services.AddTransient<IExitInfoRepository, ExitInfoRepository>();

        services.AddTransient<IBarcodeScannerCameraConfigRepository, BarcodeScannerCameraConfigRepository>();
        services.AddTransient<IPanoramaCameraConfigRepository, PanoramaCameraConfigRepository>();
        services.AddTransient<IVolumeCameraConfigRepository, VolumeCameraConfigRepository>();
        services.AddTransient<IUsbCameraConfigRepository, UsbCameraConfigRepository>();
        services.AddTransient<IConfigRepository, ConfigRepository>();
        services.AddTransient<ISettingsReader, SettingsReader>();
        services.AddTransient<ISettingsStore, SettingsStore>();
        services.AddTransient<ILogisticsCodeRecognitionRepository, LogisticsCodeRecognitionRepository>();
        services.AddTransient<IPackageExitDefinitionRepository, PackageExitDefinitionRepository>();
        services.AddTransient<ISortingInstructionBindingRepository, SortingInstructionBindingRepository>();
        services.AddTransient<ILogisticsRegexRepository, LogisticsRegexRepository>();
        services.AddTransient<ISortingInstructionRepository, SortingInstructionRepository>();
        services.AddTransient<IPackageExitLockBindingRepository, PackageExitLockBindingRepository>();
        services.AddTransient<IBarCodeSortingRepository, BarCodeSortingRepository>();
        services.AddTransient<IBarCodeRegexRepository, BarCodeRegexRepository>();
        services.AddTransient<IWeightSortingRepository, WeightSortingRepository>();
        services.AddTransient<IWeightRuleRepository, WeightRuleRepository>();
        services.AddTransient<IVolumeSortingRepository, VolumeSortingRepository>();
        services.AddTransient<IVolumeRuleRepository, VolumeRuleRepository>();
        services.AddTransient<ILogisticsSortingRepository, LogisticsSortingRepository>();
        services.AddTransient<ILogisticsRuleRepository, LogisticsRuleRepository>();
        services.AddTransient<IOcrSortingRepository, OcrSortingRepository>();
        services.AddTransient<IOcrRuleRepository, OcrRuleRepository>();
        services.AddTransient<IApiSortingRepository, ApiSortingRepository>();
        services.AddTransient<IApiRuleRepository, ApiRuleRepository>();
        services.AddTransient<ICommunicationConnectionConfigRepository, CommunicationConnectionConfigRepository>();
        services.AddTransient<IDeviceExtensionConfigRepository, DeviceExtensionConfigRepository>();
        services.AddTransient<IHeartbeatConfigRepository, HeartbeatConfigRepository>();
        services.AddTransient<ISerialPortConfigRepository, SerialPortConfigRepository>();
        services.AddTransient<ITcpConfigRepository, TcpConfigRepository>();
        services.AddTransient<ITcpConnectionConfigRepository, TcpConnectionConfigRepository>();
        services.AddTransient<INvrCameraBindingRepository, NvrCameraBindingRepository>();
        services.AddTransient<IIpcNvrConfigRepository, IpcNvrConfigRepository>();
        services.AddTransient<INvrWatermarkConfigRepository, NvrWatermarkConfigRepository>();

        services.AddTransient<IAppLogRepository, AppLogRepository>();
        services.AddTransient<ICameraLogRepository, CameraLogRepository>();
        services.AddTransient<ISortingLogRepository, SortingLogRepository>();
        services.AddTransient<IWeighingLogRepository, WeighingLogRepository>();
        services.AddTransient<IVolumeLogRepository, VolumeLogRepository>();
        services.AddTransient<IApiLogRepository, ApiLogRepository>();
        services.AddTransient<IOutputLogRepository, OutputLogRepository>();
        services.AddTransient<IInputLogRepository, InputLogRepository>();
        services.AddTransient<IOcrLogRepository, OcrLogRepository>();
        services.AddTransient<IFtpLogRepository, FtpLogRepository>();
        services.AddTransient<ICleanupLogRepository, CleanupLogRepository>();
        services.AddTransient<IExceptionLogRepository, ExceptionLogRepository>();
        return services;
    }

    /// <summary>为指定数据库文件配置统一的 SQLite 策略。</summary>
    private static void ConfigureSqlite(DbContextOptionsBuilder options, string databaseFileName) {
        var connectionString = new SqliteConnectionStringBuilder {
            DataSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, databaseFileName),
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = true,
            DefaultTimeout = 30
        }.ToString();
        options.UseSqlite(connectionString, builder => {
                builder.CommandTimeout(100);
                builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            })
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .EnableDetailedErrors(false)
            .EnableSensitiveDataLogging(false);
    }
}
