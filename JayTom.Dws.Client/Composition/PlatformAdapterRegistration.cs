using JayTom.Dws.Abstractions.Time;
using JayTom.Dws.Application.Time;
using JayTom.Dws.Camera;
using JayTom.Dws.Camera.Cameras.SmartCamera.Hikvision;
using JayTom.Dws.Client.Service.ExternalDataService.Communication.TcpComm;
using JayTom.Dws.Client.Service;
using JayTom.Dws.Client.Service.ResultOutput.Communication.TcpComm;
using JayTom.Dws.Client.Service.Sorting.Communication.SerialComm;
using JayTom.Dws.Client.Service.Sorting.Communication.TcpComm;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Infrastructure.Service;
using JayTom.Dws.Infrastructure.SignalR.CloudApi.ClientMessageHub;
using JayTom.Dws.Interface;
using JayTom.Dws.Interface.Cloud;
using JayTom.Dws.Interface.Cloud.CloudVideo;
using JayTom.Dws.Interface.License;
using JayTom.Dws.Nvr;
using JayTom.Dws.Nvr.Nvr;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Http;

namespace JayTom.Dws.Client.Composition;

/// <summary>集中注册网络、设备与第三方平台适配器。</summary>
internal static class PlatformAdapterRegistration {
    /// <summary>旧接口迁移期间使用的兼容客户端名称。</summary>
    internal const string LegacyHttpClient = "legacy-api";
    /// <summary>单次外部请求允许占用连接的最长时间。</summary>
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromMinutes(2);

    /// <summary>注册平台和硬件适配器。</summary>
    public static IServiceCollection AddDwsPlatformAdapters(this IServiceCollection services) {
        AddLongRunningHttpClient(services, LegacyHttpClient);
        AddLongRunningHttpClient(services, ApiHttpClientNames.ExternalApi);
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
        services.AddSingleton<IComputer, Infrastructure.IComputer.Computer>();
        services.AddSingleton<ICamera, HikvisionSmartCamera>();
        services.AddSingleton<IOcr, ExpressBillOcr>();
        services.AddSingleton<IDynamicScale, DefaultDynamicScale>();
        services.AddSingleton<IStaticScale, DefaultStaticScale>();
        services.AddSingleton<IGrayscaleDevice>(_ =>
            new GwGrayscaleDevice(new TouchSocketTcpClient(), new TouchSocketTcpServer()));
        services.AddSingleton<ICloud, CloudVideoUploadApi>();
        services.AddSingleton<INvrManager, DaHuaNvr>();
        services.AddSingleton<IClientLicenseApi, DefaultClientLicenseApi>();
        services.AddSingleton<ICloudApiClientMessageHub, CloudApiClientMessageHub>();
        services.AddDwsUploadProviders();
        return services;
    }

    /// <summary>注册带连接超时、DNS 轮换和空闲连接回收的长期运行 HTTP 客户端。</summary>
    private static void AddLongRunningHttpClient(IServiceCollection services, string clientName) {
        services.AddHttpClient(clientName, client => client.Timeout = RequestTimeout)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler {
                MaxConnectionsPerServer = 100,
                ConnectTimeout = TimeSpan.FromSeconds(15),
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                AutomaticDecompression = DecompressionMethods.GZip |
                                         DecompressionMethods.Deflate |
                                         DecompressionMethods.Brotli
            });
    }
}
