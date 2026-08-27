using JayTom.Dws.Infrastructure.SignalR.CloudApi.ClientMessageHub;
using JayTom.Dws.Application.Diagnostics;
using JayTom.Dws.Infrastructure.Diagnostics;
using JayTom.Dws.Infrastructure.Configuration;
using JayTom.Dws.Application.Configuration.Secrets;
using JayTom.Dws.Application.Deployment;
using JayTom.Dws.Application.Storage;
using JayTom.Dws.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace JayTom.Dws.Infrastructure.DependencyInjection;

/// <summary>集中注册由基础设施层拥有的操作系统和云消息适配器。</summary>
public static class PlatformInfrastructureServiceCollectionExtensions
{
    /// <summary>把基础设施契约绑定到其具体实现。</summary>
    public static IServiceCollection AddDwsInfrastructurePlatformAdapters(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<
            global::JayTom.Dws.Infrastructure.IComputer.IComputer,
            global::JayTom.Dws.Infrastructure.IComputer.Computer>();
        services.AddSingleton<IDiskInventory, ComputerDiskInventory>();
        services.AddSingleton<ICloudApiClientMessageHub, CloudApiClientMessageHub>();
        services.AddSingleton(_ => CreatePathOptions());
        services.AddSingleton<IApplicationPathProvider, DefaultApplicationPathProvider>();
        services.AddSingleton<ISecretKeyProvider, EnvironmentSecretKeyProvider>();
        services.AddSingleton<ISecretStore, EncryptedFileSecretStore>();
        services.AddSingleton<IBinaryAssetStore, FileBinaryAssetStore>();
        return services;
    }

    /// <summary>创建不依赖当前工作目录的默认运行时路径。</summary>
    private static ApplicationPathOptions CreatePathOptions()
    {
        var applicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        var root = Path.Combine(applicationData, "JayTom", "Dws");
        return new ApplicationPathOptions
        {
            DataDirectory = Path.Combine(root, "data"),
            ConfigurationDirectory = Path.Combine(root, "configuration"),
            LogDirectory = Path.Combine(root, "logs"),
            ModelDirectory = Path.Combine(root, "models"),
            AdapterPackDirectory = Path.Combine(AppContext.BaseDirectory, "adapters")
        };
    }
}
