using NLog.Web;
using System.Net;
using JayTom.Dws.Plugin.Ftp;
using NLog.Extensions.Logging;
using JayTom.Dws.Plugin.SaveImage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using JayTom.Dws.CrossCutting.SignalR;
using JayTom.Dws.DataInteractionService;
using JayTom.Dws.Domain.Repository.LocalLog;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.DataInteractionService.SignalR;
using JayTom.Dws.DataInteractionService.Services;
using JayTom.Dws.Infrastructure.Repository.LocalLog;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.Infrastructure.Service.ImageService;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig.ConnectionParams;

internal class Program {
    private static DataInteractionSettings _settings = new();

    private static void Main(string[] args) {
        IConfiguration configuration = new ConfigurationBuilder()
             .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
             .Build();
        _settings = configuration.GetSection(nameof(DataInteractionSettings)).Get<DataInteractionSettings>() ?? new DataInteractionSettings();
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder => {
                webBuilder.UseKestrel()
                    .UseUrls(_settings.BaseUrl)
                    .ConfigureServices((hostContext, services) => {
                        services.AddSingleton<IBaseServerMessageHub, BaseServerMessageHub>();
                        //用到的服务
                        {
                            services.AddSingleton<IPackageRepository, PackageRepository>();
                            services.AddSingleton<IConfigRepository, ConfigRepository>();
                            services.AddSingleton<IBarCodeRepository, BarCodeRepository>();
                            services.AddSingleton<IWeightRepository, WeightRepository>();
                            services.AddSingleton<IVolumeRepository, VolumeRepository>();
                            services.AddSingleton<IUploadRepository, UploadRepository>();
                            services.AddSingleton<IExitInfoRepository, ExitInfoRepository>();
                            services.AddSingleton<ISortingRepository, SortingRepository>();
                            services.AddSingleton<IOcrRepository, OcrRepository>();
                            services.AddSingleton<IImageRepository, ImageRepository>();
                            services.AddSingleton<ICloudVideoUploadRepository, CloudVideoUploadRepository>();
                            services.AddSingleton<IBarcodeScannerCameraConfigRepository, BarcodeScannerCameraConfigRepository>();
                            services.AddSingleton<IPanoramaCameraConfigRepository, PanoramaCameraConfigRepository>();
                            services.AddSingleton<IUsbCameraConfigRepository, UsbCameraConfigRepository>();
                            services.AddSingleton<IVolumeCameraConfigRepository, VolumeCameraConfigRepository>();
                            services.AddSingleton<INvrCameraBindingRepository, NvrCameraBindingRepository>();
                            services.AddSingleton<ICommunicationConnectionConfigRepository, CommunicationConnectionConfigRepository>();
                            services.AddSingleton<IApiSortingRepository, ApiSortingRepository>();
                            services.AddSingleton<IBarCodeSortingRepository, BarCodeSortingRepository>();
                            services.AddSingleton<ILogisticsCodeRecognitionRepository, LogisticsCodeRecognitionRepository>();
                            services.AddSingleton<ILogisticsSortingRepository, LogisticsSortingRepository>();
                            services.AddSingleton<IOcrSortingRepository, OcrSortingRepository>();
                            services.AddSingleton<IPackageExitDefinitionRepository, PackageExitDefinitionRepository>();
                            services.AddSingleton<IPackageExitLockBindingRepository, PackageExitLockBindingRepository>();
                            services.AddSingleton<ISortingInstructionBindingRepository, SortingInstructionBindingRepository>();
                            services.AddSingleton<ISortingInstructionRepository, SortingInstructionRepository>();
                            services.AddSingleton<IVolumeSortingRepository, VolumeSortingRepository>();
                            services.AddSingleton<IWeightSortingRepository, WeightSortingRepository>();
                            services.AddSingleton<IApiLogRepository, ApiLogRepository>();
                            services.AddSingleton<IAppLogRepository, AppLogRepository>();
                            services.AddSingleton<ICameraLogRepository, CameraLogRepository>();
                            services.AddSingleton<IExceptionLogRepository, ExceptionLogRepository>();
                            services.AddSingleton<IFtpLogRepository, FtpLogRepository>();
                            services.AddSingleton<IInputLogRepository, InputLogRepository>();
                            services.AddSingleton<IOcrLogRepository, OcrLogRepository>();
                            services.AddSingleton<IOutputLogRepository, OutputLogRepository>();
                            services.AddSingleton<ISortingLogRepository, SortingLogRepository>();
                            services.AddSingleton<IVolumeLogRepository, VolumeLogRepository>();
                            services.AddSingleton<IWeighingLogRepository, WeighingLogRepository>();
                            services.AddSingleton<IImageStorageService, DefaultImageStorageService>();

                            // 这里是添加的额外服务
                            services.AddSingleton<ISaveImage, SaveImage>();
                            services.AddSingleton<IFtp, FluentFtpClient>();
                        }
                        services.AddSingleton<IDataInteractionServiceMessageHub, DataInteractionServiceMessageHub>();
                        //IDataInteractionServiceMessageHub
                        services.AddMvc();

                        // 添加 SignalR 服务
                        services.AddSignalR(options => {
                            options.HandshakeTimeout = TimeSpan.FromMinutes(1);
                            options.EnableDetailedErrors = true;
                            options.MaximumReceiveMessageSize = null;
                            options.KeepAliveInterval = TimeSpan.FromMinutes(1);
                            options.ClientTimeoutInterval = TimeSpan.FromMinutes(5);
                            options.MaximumParallelInvocationsPerClient = 10;
                            options.StreamBufferCapacity = int.MaxValue;
                        });
                    })
                    .Configure(app => {
                        app.UseRouting();

                        // 配置 SignalR Hub 终结点
                        app.UseEndpoints(endpoints => {
                            endpoints.MapHub<BaseServerMessageHub>("/Message", options => {
                                options.TransportMaxBufferSize = 0;
                                options.ApplicationMaxBufferSize = 0;
                                options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(10);
                            });

                            // 可以添加其他端点配置
                        });
                    });
            })
            .ConfigureServices(services => {
                services.AddHostedService<DataInteractionService>();
                services.AddHostedService<CleanupService>();
            })
            .UseWindowsService() // 配置为Windows服务
            .ConfigureLogging(logging => {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Trace);
                logging.AddNLog();
            })
            .UseNLog();

        var host = hostBuilder.Build();
        host.Run();
    }
}