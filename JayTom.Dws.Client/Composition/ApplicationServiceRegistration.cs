using JayTom.Dws.Client.Service;
using JayTom.Dws.Client.Service.DefaultConfiguration;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.Service.ExternalDataService;
using JayTom.Dws.Client.Service.ImageService;
using JayTom.Dws.Client.Service.ResultOutput;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Client.Service.SyncSettings;
using JayTom.Dws.Domain.Service.CacheCleanup;
using JayTom.Dws.Domain.Service.ImageService;
using Microsoft.Extensions.DependencyInjection;
using JayTom.Dws.Infrastructure.Service;
using JayTom.Dws.Application.Messaging;
using JayTom.Dws.Client.EventMediators;

namespace JayTom.Dws.Client.Composition;

/// <summary>集中注册桌面端应用服务。</summary>
internal static class ApplicationServiceRegistration {
    /// <summary>注册设备、分拣、输出和同步等应用服务。</summary>
    public static IServiceCollection AddDwsApplicationServices(this IServiceCollection services) {
        services.AddSingleton(EventAggregator.Instance);
        services.AddSingleton<IEventBus>(provider => provider.GetRequiredService<EventAggregator>());
        services.AddSingleton<IPackageSessionStore, PackageSessionStore>();
        services.AddSingleton<IComputerInfoReporter, ComputerInfoReporter>();
        services.AddSingleton<IDefaultConfigurationService, DefaultConfigurationService>();
        services.AddSingleton<IDeviceService, DefaultDeviceService>();
        services.AddSingleton<IImageStorageService, DefaultImageStorageService>();
        services.AddSingleton<IResultOutputService, DefaultResultOutputService>();
        services.AddSingleton<IExternalDataService, ExternalDataService>();
        services.AddSingleton<ICacheCleanupService, CacheCleanupService>();
        services.AddSingleton<ISortingService, DefaultSortingService>();
        services.AddSingleton<IExitMonitor, DefaultExitMonitor>();
        services.AddSingleton<IStackedPackageService, DefaultStackedPackageService>();
        services.AddSingleton<ISortingConnectionService, DefaultSortingConnectionService>();
        services.AddSingleton<IGrayscaleService, DefaultGrayscaleService>();
        services.AddSingleton<ISyncSettingsService, SyncSettingsService>();
        return services;
    }
}
