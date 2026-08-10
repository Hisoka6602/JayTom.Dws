using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Client.Service.ProcessingServices;
using JayTom.Dws.Client.Service.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace JayTom.Dws.Client.Composition;

/// <summary>集中注册随应用生命周期运行的后台工作流。</summary>
internal static class HostedWorkflowRegistration {
    /// <summary>注册全部生产后台工作流。</summary>
    public static IServiceCollection AddDwsHostedWorkflows(this IServiceCollection services) {
        services.AddSingleton<IHostedServiceSupervisor, HostedServiceSupervisor>();
        // 先启动消费者、最后启动主生产流程；停止时监督器按逆序关闭，确保尾部数据可被消费。
        services.AddHostedService<LogProcessingService>();
        services.AddHostedService<DataProcessingBackgroundService>();
        services.AddHostedService<SubmitApiBackgroundService>();
        services.AddHostedService<SaveImageBackgroundService>();
        services.AddHostedService<CleanupService>();
        services.AddHostedService<ComputerInfoBackgroundService>();
        services.AddHostedService<SingleInstanceBackgroundService>();
        services.AddHostedService<TimerBackgroundService>();
        services.AddHostedService<CloudBackgroundService>();
        services.AddHostedService<PackageExitUpdateBackgroundService>();
        services.AddHostedService<YunShanPackageBackgroundService>();
        return services;
    }
}
