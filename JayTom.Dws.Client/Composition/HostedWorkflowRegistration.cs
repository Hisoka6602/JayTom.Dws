using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Client.Service.ProcessingServices;
using JayTom.Dws.Client.Service.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JayTom.Dws.Client.Composition;

/// <summary>集中注册随应用生命周期运行的后台工作流。</summary>
internal static class HostedWorkflowRegistration {
    /// <summary>注册全部生产后台工作流。</summary>
    public static IServiceCollection AddDwsHostedWorkflows(this IServiceCollection services) {
        services.AddSingleton<IHostedServiceSupervisor, HostedServiceSupervisor>();
        // 先启动消费者、最后启动主生产流程；停止时监督器按逆序关闭，确保尾部数据可被消费。
        services.AddSupervisedHostedService<LogProcessingService>();
        services.AddSupervisedHostedService<DataProcessingBackgroundService>();
        services.AddSupervisedHostedService<SubmitApiBackgroundService>();
        services.AddSupervisedHostedService<SaveImageBackgroundService>();
        services.AddSupervisedHostedService<CleanupService>();
        services.AddSupervisedHostedService<ComputerInfoBackgroundService>();
        services.AddSupervisedHostedService<SingleInstanceBackgroundService>();
        services.AddSupervisedHostedService<TimerBackgroundService>();
        services.AddSupervisedHostedService<CloudBackgroundService>();
        services.AddSupervisedHostedService<PackageExitUpdateBackgroundService>();
        services.AddSupervisedHostedService<YunShanPackageBackgroundService>();
        return services;
    }

    /// <summary>将后台服务注册为每次运行使用独立依赖注入作用域的新实例。</summary>
    private static IServiceCollection AddSupervisedHostedService<TService>(
        this IServiceCollection services)
        where TService : class, IHostedService {
        services.AddTransient<TService>();
        services.AddSingleton(SupervisedHostedServiceDescriptor.Create<TService>());
        return services;
    }
}
