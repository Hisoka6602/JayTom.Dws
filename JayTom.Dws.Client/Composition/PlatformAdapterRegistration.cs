using JayTom.Dws.Abstractions.Time;
using JayTom.Dws.Application.Time;
using JayTom.Dws.Camera;
using JayTom.Dws.Camera.Cameras.SmartCamera.Hikvision;
using JayTom.Dws.Client.Service.ExternalDataService.Communication.TcpComm;
using JayTom.Dws.Client.Service;
using JayTom.Dws.Client.Service.ResultOutput.Communication.TcpComm;
using JayTom.Dws.Client.Service.Sorting.Communication.SerialComm;
using JayTom.Dws.Client.Service.Sorting.Communication.TcpComm;
using JayTom.Dws.Infrastructure.DependencyInjection;
using JayTom.Dws.Integrations;
using JayTom.Dws.Integrations.Cloud;
using JayTom.Dws.Integrations.Cloud.CloudVideo;
using JayTom.Dws.Integrations.License;
using JayTom.Dws.Camera.Nvr.Legacy;
using JayTom.Dws.Ocr;
using JayTom.Dws.Ocr.ExpressBill;
using JayTom.Dws.Plugin;
using JayTom.Dws.Plugin.Device.GrayscaleDevice;
using JayTom.Dws.Plugin.Device.KeyboardDevice;
using JayTom.Dws.Plugin.Excel;
using JayTom.Dws.Abstractions.Integrations.Ftp;
using JayTom.Dws.Plugin.Ftp;
using JayTom.Dws.Plugin.SaveImage;
using JayTom.Dws.Plugin.Scale.DynamicScale;
using JayTom.Dws.Plugin.Scale.StaticScale;
using JayTom.Dws.Plugin.Speech;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;
using JayTom.Dws.Plugin.Contracts;
using JayTom.Dws.Plugin.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Composition;

/// <summary>集中注册网络、设备与第三方平台适配器。</summary>
internal static class PlatformAdapterRegistration {
    /// <summary>注册平台和硬件适配器。</summary>
    public static IServiceCollection AddDwsPlatformAdapters(this IServiceCollection services) {
        services.AddDwsInfrastructurePlatformAdapters();
        services.AddDwsIntegrationHttpClient();
        services.AddMemoryCache();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build());
        services.AddSingleton<IClock, SystemClock>();

        services.AddSingleton<IExcel, NpoiExport>();
        services.AddSingleton<IFtp, FluentFtpClient>();
        services.AddSingleton<ISaveImage, SaveImage>();
        services.AddSingleton<ISpeech, Speech>();
        services.AddSingleton<ITcpCommClient, TouchSocketTcpClient>();
        services.AddSingleton<ITcpCommServer, TouchSocketTcpServer>();
        services.AddSingleton<ITcpContentOutput>(_ =>
            new TcpContentOutput(new TouchSocketTcpClient(), new TouchSocketTcpServer()));
        services.AddSingleton<ITcpVolumeInput>(_ =>
            new TcpVolumeInput(new TouchSocketTcpClient(), new TouchSocketTcpServer()));
        services.AddSingleton<ITcpContentInput>(_ =>
            new TcpContentInput(new TouchSocketTcpClient(), new TouchSocketTcpServer()));
        services.AddSingleton<ISortingSerialPort>(_ => new SortingSerialPort(new SerialPort()));
        services.AddSingleton<ISortingTcp>(_ =>
            new SortingTcp(new TouchSocketTcpClient(), new TouchSocketTcpServer()));
        services.AddSingleton<IKeyboardDeviceManager, KeyboardDeviceManager>();
        services.AddSingleton<IPackageDetectionSerialPort>(_ =>
            new PackageDetectionSerialPort(new SerialPort()));
        services.AddSingleton<IPackageDetectionTcp>(_ =>
            new PackageDetectionTcp(new TouchSocketTcpClient(), new TouchSocketTcpServer()));

        services.AddSingleton<INetworkTime, NetworkTime>();
        services.AddSingleton<ICertificateValidationService, CertificateValidationService>();
        services.AddSingleton<ICamera, HikvisionSmartCamera>();
        services.AddSingleton<IOcr, ExpressBillOcr>();
        services.AddSingleton<IDynamicScale, DefaultDynamicScale>();
        services.AddSingleton<IStaticScale, DefaultStaticScale>();
        services.AddSingleton<IGrayscaleDevice>(_ =>
            new GwGrayscaleDevice(new TouchSocketTcpClient(), new TouchSocketTcpServer()));
        services.AddSingleton<ICloud, CloudVideoUploadApi>();
        services.AddSingleton<INvrManager, DaHuaNvr>();
        services.AddSingleton<IClientLicenseApi, DefaultClientLicenseApi>();
        services.AddSingleton<IPluginManifestValidator, PluginManifestValidator>();
        services.AddSingleton(_ => new PluginTrustOptions {
            TrustDirectory = Path.Combine(AppContext.BaseDirectory, "plugin-trust"),
            RevokedKeyIds = ReadEnvironmentSet("DWS_PLUGIN_REVOKED_KEY_IDS"),
            AllowedPermissions = ReadEnvironmentSet("DWS_PLUGIN_ALLOWED_PERMISSIONS")
        });
        services.AddSingleton<IPluginPackageVerifier, PluginPackageVerifier>();
        services.AddSingleton(provider => new PluginRuntime(
            provider.GetRequiredService<IPluginManifestValidator>(),
            Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0),
            contractMajorVersion: 1,
            provider.GetRequiredService<IPluginPackageVerifier>()));
        services.AddDwsUploadProviders();
        return services;
    }

    /// <summary>读取部署环境注入的分号或逗号分隔集合。</summary>
    private static IReadOnlySet<string> ReadEnvironmentSet(string variableName) =>
        new HashSet<string>(
            Environment.GetEnvironmentVariable(variableName)?.Split(
                [',', ';'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [],
            StringComparer.OrdinalIgnoreCase);
}
