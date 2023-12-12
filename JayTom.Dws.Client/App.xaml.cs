using DryIoc;
using System;
using Prism.Ioc;
using System.IO;
using Prism.Mvvm;
using Prism.DryIoc;
using System.Windows;
using JayTom.Dws.Ocr;
using Newtonsoft.Json;
using System.IO.Pipes;
using System.Net.Http;
using System.Threading;
using JayTom.Dws.Camera;
using JayTom.Dws.Plugin;
using JayTom.Dws.Interface;
using System.Globalization;
using System.Windows.Media;
using JayTom.Dws.Plugin.Ftp;
using System.Threading.Tasks;
using System.Windows.Interop;
using JayTom.Dws.Client.Views;
using JayTom.Dws.Plugin.Excel;
using JayTom.Dws.Plugin.Speech;
using System.Windows.Threading;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Client.Service;
using JayTom.Dws.Infrastructure;
using JayTom.Dws.Ocr.ExpressBill;
using JayTom.Dws.Interface.Cloud;
using JayTom.Dws.Plugin.SaveImage;
using JayTom.Dws.Client.ViewModels;
using Microsoft.Extensions.Hosting;
using JayTom.Dws.Client.Views.Pages;
using Microsoft.EntityFrameworkCore;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.Views.Editors;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Infrastructure.Service;
using JayTom.Dws.Client.ViewModels.Pages;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Plugin.Scale.StaticScale;
using JayTom.Dws.Client.ViewModels.Editors;
using JayTom.Dws.Plugin.Scale.DynamicScale;
using JayTom.Dws.Domain.Repository.LocalLog;
using JayTom.Dws.Interface.Cloud.CloudVideo;
using JayTom.Dws.Client.Service.ImageStorage;
using JayTom.Dws.Client.Service.ResultOutput;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Service.CacheCleanup;
using Microsoft.Extensions.DependencyInjection;
using JayTom.Dws.Client.Views.Pages.Preferences;
using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Client.Service.ExternalDataService;
using JayTom.Dws.Infrastructure.Repository.LocalLog;
using DryIoc.Microsoft.DependencyInjection.Extension;
using JayTom.Dws.Client.ViewModels.Pages.Preferences;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.Client.Service.DefaultConfiguration;
using JayTom.Dws.Camera.Cameras.SmartCamera.Hikvision;
using JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.Views;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Client.Views.Pages.Preferences.LogsViews;
using JayTom.Dws.Client.Views.Pages.Preferences.AppSettings;
using JayTom.Dws.Client.Views.Pages.Preferences.CloudService;
using JayTom.Dws.Client.Service.Sorting.Communication.TcpComm;
using JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.ViewModels;
using JayTom.Dws.Client.Service.Sorting.Communication.SerialComm;
using JayTom.Dws.Client.Views.Pages.Preferences.ApiConfiguration;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.AppSettings;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.CloudService;
using JayTom.Dws.Client.Service.ResultOutput.Communication.TcpComm;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.LogsViewModel;
using JayTom.Dws.Client.Views.Pages.Preferences.CameraConfiguration;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration;
using JayTom.Dws.Client.Service.ExternalDataService.Communication.TcpComm;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.Views.Pages.Preferences.PackageSortingConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig.ConnectionParams;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Client.Views.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages;

namespace JayTom.Dws.Client {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication {
        private IHost? _host;
        private Mutex? _singleInstanceMutex;
        private NamedPipeServerStream? pipeServer;
        private const string PipeName = "DwsPipe";

        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            //注册窗口
            containerRegistry.RegisterDialog<ApiAccessDialog>();
            containerRegistry.RegisterDialog<ApiTestDialog>();
            containerRegistry.RegisterDialog<BarCodeDetailsDialog>();

            //插件窗口
            {
                containerRegistry.RegisterDialog<SunnenInputBarcodeControl>();
            }
            //设置窗口
            {
                containerRegistry.RegisterDialog<VideoCameraSettingsDialog>();
            }
            //海康软硬触发
            {
                containerRegistry.RegisterDialog<TriggerModeSelectionPage>();
            }
            //全景指定触发
            {
                containerRegistry.RegisterDialog<ScanCameraSelectionDialog>();
            }
            //跳转注册
            {
                containerRegistry.RegisterForNavigation<PluginMarketplacePage>();
                containerRegistry.RegisterForNavigation<DataManagementPage>();
                containerRegistry.RegisterForNavigation<CameraConfigurationPage>();
                containerRegistry.RegisterForNavigation<APISettingsPage>();
                containerRegistry.RegisterForNavigation<SaveImageSettingsPage>();
                containerRegistry.RegisterForNavigation<BarcodeFilterSettingsPage>();
                containerRegistry.RegisterForNavigation<ResultOutputSettingsPage>();
                containerRegistry.RegisterForNavigation<ContentInputSettingsPage>();
                containerRegistry.RegisterForNavigation<CacheClearSettingsPage>();
                containerRegistry.RegisterForNavigation<WeightSettingPages>();
                containerRegistry.RegisterForNavigation<VolumeSettingsPage>();
                containerRegistry.RegisterForNavigation<LogManagerPage>();
                containerRegistry.RegisterForNavigation<PackageSortingSettingsPage>();
                containerRegistry.RegisterForNavigation<OcrSettingsPage>();
                containerRegistry.RegisterForNavigation<WorkflowSettingsPage>();
                containerRegistry.RegisterForNavigation<AppSettingsPage>();
                containerRegistry.RegisterForNavigation<CloudServicePage>();
                //LogManagerPage
                //相机
                containerRegistry.RegisterForNavigation<BarcodeScannerCameraConfigPage>();
                containerRegistry.RegisterForNavigation<CameraFinderPage>();
                containerRegistry.RegisterForNavigation<PanoramaCameraConfigPage>();
                containerRegistry.RegisterForNavigation<VolumeCameraConfigPage>();
                //分拣设置
                containerRegistry.RegisterForNavigation<LogisticsCodeRecognitionPage>();
                containerRegistry.RegisterForNavigation<PackageExitDefinitionPage>();
                containerRegistry.RegisterForNavigation<SortingInstructionBindingPage>();
                containerRegistry.RegisterForNavigation<SortingSchemeSettingsPage>();
                containerRegistry.RegisterForNavigation<CommunicationsSettingsPage>();
                containerRegistry.RegisterForNavigation<SortingMethodPage>();
                //程序设置
                containerRegistry.RegisterForNavigation<GridSettingsPage>();
                containerRegistry.RegisterForNavigation<OtherSettingsPage>();
                //云端服务
                containerRegistry.RegisterForNavigation<CloudDataPage>();
                containerRegistry.RegisterForNavigation<CloudVideoPage>();
            }
            //其他注册
            containerRegistry.GetContainer().RegisterServices(services => {
                services.AddPooledDbContextFactory<SqliteContext>(options => {
                    options.UseSqlite(
                        $"Data Source={System.AppDomain.CurrentDomain.BaseDirectory}Data.db",
                        builder => {
                            builder.CommandTimeout(100); //180秒超时
                            builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                        }).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                }, 300);

                services.AddPooledDbContextFactory<SqliteConfContext>(options => {
                    options.UseSqlite(
                        $"Data Source={System.AppDomain.CurrentDomain.BaseDirectory}Configuration.db",
                        builder => {
                            builder.CommandTimeout(100); //180秒超时
                            builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                        }).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                }, 300);
                services.AddPooledDbContextFactory<SqliteLogsContext>(options => {
                    options.UseSqlite(
                        $"Data Source={System.AppDomain.CurrentDomain.BaseDirectory}ClientLogs.db",
                        builder => {
                            builder.CommandTimeout(100); //180秒超时
                            builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                        }).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                }, 300);
                //http
                services.AddHttpClient("INSURANCE", httpClient => {
                    // httpClient.Timeout = TimeSpan.FromSeconds(10);
                }).ConfigurePrimaryHttpMessageHandler(() => {
                    var handler = new HttpClientHandler() {
                        UseDefaultCredentials = true,
                        MaxConnectionsPerServer = 600,
                        ServerCertificateCustomValidationCallback = (m, c, ch, _) => true,
                        //UseProxy = false
                    };

                    return handler;
                });

                //配置内存缓存
                services.AddMemoryCache();
                //本地数据表注册
                //data

                services.AddScoped<IBarCodeRepository, BarCodeRepository>();
                services.AddScoped<ISoundRepository, SoundRepository>();
                services.AddScoped<IVolumeRepository, VolumeRepository>();
                services.AddScoped<IWeightRepository, WeightRepository>();
                services.AddScoped<IUploadRepository, UploadRepository>();
                services.AddScoped<ISortingRepository, SortingRepository>();
                services.AddScoped<IOcrRepository, OcrRepository>();
                services.AddScoped<IImageRepository, ImageRepository>();
                services.AddScoped<ICloudVideoUploadRepository, CloudVideoUploadRepository>();
                //config
                services.AddScoped<IBarcodeScannerCameraConfigRepository, BarcodeScannerCameraConfigRepository>();
                services.AddScoped<IPanoramaCameraConfigRepository, PanoramaCameraConfigRepository>();
                services.AddScoped<IVolumeCameraConfigRepository, VolumeCameraConfigRepository>();
                services.AddScoped<IConfigRepository, ConfigRepository>();
                services.AddScoped<ILogisticsCodeRecognitionRepository, LogisticsCodeRecognitionRepository>();
                services.AddScoped<IPackageExitDefinitionRepository, PackageExitDefinitionRepository>();
                services.AddScoped<ISortingInstructionBindingRepository, SortingInstructionBindingRepository>();
                services.AddScoped<ILogisticsRegexRepository, LogisticsRegexRepository>();
                services.AddScoped<ISortingInstructionRepository, SortingInstructionRepository>();

                services.AddScoped<IBarCodeSortingRepository, BarCodeSortingRepository>();
                services.AddScoped<IBarCodeRegexRepository, BarCodeRegexRepository>();

                services.AddScoped<IWeightSortingRepository, WeightSortingRepository>();
                services.AddScoped<IWeightRuleRepository, WeightRuleRepository>();

                services.AddScoped<IVolumeSortingRepository, VolumeSortingRepository>();
                services.AddScoped<IVolumeRuleRepository, VolumeRuleRepository>();

                services.AddScoped<ILogisticsSortingRepository, LogisticsSortingRepository>();
                services.AddScoped<ILogisticsRuleRepository, LogisticsRuleRepository>();

                services.AddScoped<IOcrSortingRepository, OcrSortingRepository>();
                services.AddScoped<IOcrRuleRepository, OcrRuleRepository>();

                services.AddScoped<IApiSortingRepository, ApiSortingRepository>();
                services.AddScoped<IApiRuleRepository, ApiRuleRepository>();

                services.AddScoped<ICommunicationConnectionConfigRepository, CommunicationConnectionConfigRepository>();
                services.AddScoped<IDeviceExtensionConfigRepository, DeviceExtensionConfigRepository>();
                services.AddScoped<IHeartbeatConfigRepository, HeartbeatConfigRepository>();
                services.AddScoped<ISerialPortConfigRepository, SerialPortConfigRepository>();
                services.AddScoped<ITcpConfigRepository, TcpConfigRepository>();
                services.AddScoped<ITcpConnectionConfigRepository, TcpConnectionConfigRepository>();
                //logs
                services.AddScoped<IAppLogRepository, AppLogRepository>();
                services.AddScoped<ICameraLogRepository, CameraLogRepository>();
                services.AddScoped<ISortingLogRepository, SortingLogRepository>();
                services.AddScoped<IWeighingLogRepository, WeighingLogRepository>();
                services.AddScoped<IVolumeLogRepository, VolumeLogRepository>();
                services.AddScoped<IApiLogRepository, ApiLogRepository>();
                services.AddScoped<IOutputLogRepository, OutputLogRepository>();
                services.AddScoped<IInputLogRepository, InputLogRepository>();
                services.AddScoped<IOcrLogRepository, OcrLogRepository>();
                services.AddScoped<IFtpLogRepository, FtpLogRepository>();
                services.AddScoped<ICleanupLogRepository, CleanupLogRepository>();
                services.AddScoped<IExceptionLogRepository, ExceptionLogRepository>();
                //插件注册
                services.AddScoped<IExcel, NpoiExport>();
                services.AddScoped<IFtp, FluentFtpClient>();
                services.AddScoped<ISaveImage, SaveImage>();
                services.AddScoped<ISpeech, Speech>();
                services.AddScoped<ITcpCommClient, TouchSocketTcpClient>();
                services.AddScoped<ITcpCommServer, TouchSocketTcpServer>();
                services.AddSingleton<ITcpContentOutput>(provider => new TcpContentOutput(new TouchSocketTcpClient(), new TouchSocketTcpServer()));
                services.AddSingleton<ITcpVolumeInput>(provider => new TcpVolumeInput(new TouchSocketTcpClient(), new TouchSocketTcpServer()));
                services.AddScoped<ISortingSerialPort, SortingSerialPort>();
                services.AddSingleton<ISortingTcp>(provider => new SortingTcp(new TouchSocketTcpClient(), new TouchSocketTcpServer()));
                //效验注册
                services.AddScoped<INetworkTime, NetworkTime>();
                services.AddScoped<ICertificateValidationService, CertificateValidationService>();
                //电脑注册
                services.AddScoped<IComputer, Infrastructure.IComputer.Computer>();
                //电脑信息上报
                services.AddScoped<IComputerInfoReporter, ComputerInfoReporter>();
                //写默认配置
                services.AddScoped<IDefaultConfigurationService, DefaultConfigurationService>();
                //设备注册
                services.AddScoped<ICamera, HikvisionSmartCamera>();
                //Ocr
                services.AddScoped<IOcr, ExpressBillOcr>();
                //磅秤
                services.AddScoped<IDynamicScale, DefaultDynamicScale>();
                services.AddScoped<IStaticScale, DefaultStaticScale>();
                services.AddScoped<IDeviceService, DefaultDeviceService>();
                services.AddScoped<IImageStorageService, DefaultImageStorageService>();
                services.AddScoped<IResultOutputService, DefaultResultOutputService>();
                services.AddScoped<IExternalDataService, ExternalDataService>();
                //基础服务注册
                services.AddScoped<ICacheCleanupService, CacheCleanupService>();
                //分拣注册
                services.AddScoped<ISortingService, DefaultSortingService>();
                //分拣指令服务
                //services.AddScoped<IInventoryManagementService, DefaultInventoryManagementService>();
                services.AddScoped<ISortingConnectionService, DefaultSortingConnectionService>();
                //云视频云端
                services.AddScoped<ICloud, CloudVideoUploadApi>();
            });
        }

        protected override Window CreateShell() {
            return Container.Resolve<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e) {
            _singleInstanceMutex = new Mutex(true, "Dws.Client", out var createdNew);

            if (!createdNew) {
                // 另一个实例已经在运行，尝试激活它的窗口
                NotifyExistingInstance();
                Environment.Exit(0);
            }
            ThreadPool.SetMinThreads(300, 200);
            this.DispatcherUnhandledException += delegate (object sender, DispatcherUnhandledExceptionEventArgs args) {
                //异常触发
                NLog.LogManager.GetCurrentClassLogger().Error($"{JsonConvert.SerializeObject(args.Exception)}");
                EventAggregator.Instance.Publish(new AppLogInfoModel {
                    CreateTime = DateTime.Now,
                    Message = args.Exception.Message,
                    Type = LogType.Exception
                });
            };
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args) {
                //异常触发
                NLog.LogManager.GetCurrentClassLogger().Error($"{JsonConvert.SerializeObject(args.ExceptionObject)}");
                EventAggregator.Instance.Publish(new AppLogInfoModel {
                    CreateTime = DateTime.Now,
                    Message = args?.ExceptionObject?.ToString() ?? string.Empty,
                    Type = LogType.Exception
                });
            };

            base.OnStartup(e);

            var container = Container.GetContainer();

            {
                //在这里写默认配置
                var service = container.GetService<IDefaultConfigurationService>();
                service?.WriteDefaultConfiguration().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            //加载语言
            var configRepository = container.Resolve<IConfigRepository>();
            var configInfoModel = configRepository?.FirstOrDefault(f =>
                f.ConfigName.Equals("Language")).GetAwaiter().GetResult();
            if (configInfoModel != null) {
                var culture = new CultureInfo(configInfoModel.Value);
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }

            // 创建主机并注册后台服务
            Task.Run(() => {
                // 启用硬件加速
                RenderOptions.ProcessRenderMode = RenderMode.Default;
                var container1 = Container.GetContainer();
                _host = Host.CreateDefaultBuilder()
                    .ConfigureServices((hostContext, services) => {
                        services.AddSingleton(container1.Resolve<IHttpClientFactory>());

                        //data
                        services.AddSingleton(container1.Resolve<IBarCodeRepository>());
                        services.AddSingleton(container1.Resolve<IPanoramaCameraConfigRepository>());
                        services.AddSingleton(container1.Resolve<ISoundRepository>());

                        services.AddSingleton(container1.Resolve<IVolumeRepository>());
                        services.AddSingleton(container1.Resolve<IWeightRepository>());
                        services.AddSingleton(container1.Resolve<IUploadRepository>());
                        services.AddSingleton(container1.Resolve<ISortingRepository>());
                        services.AddSingleton(container1.Resolve<IOcrRepository>());
                        services.AddSingleton(container1.Resolve<IImageRepository>());
                        services.AddSingleton(container1.Resolve<ICloudVideoUploadRepository>());
                        //config
                        services.AddSingleton(container1.Resolve<IBarcodeScannerCameraConfigRepository>());
                        services.AddSingleton(container1.Resolve<IPanoramaCameraConfigRepository>());
                        services.AddSingleton(container1.Resolve<IVolumeCameraConfigRepository>());
                        services.AddSingleton(container1.Resolve<IConfigRepository>());
                        services.AddSingleton(container1.Resolve<ILogisticsCodeRecognitionRepository>());
                        services.AddSingleton(container1.Resolve<ILogisticsCodeRecognitionRepository>());
                        services.AddSingleton(container1.Resolve<IPackageExitDefinitionRepository>());
                        services.AddSingleton(container1.Resolve<ISortingInstructionBindingRepository>());

                        services.AddSingleton(container1.Resolve<ILogisticsRegexRepository>());
                        services.AddSingleton(container1.Resolve<ISortingInstructionRepository>());

                        services.AddSingleton(container1.Resolve<IBarCodeSortingRepository>());
                        services.AddSingleton(container1.Resolve<IBarCodeRegexRepository>());

                        services.AddSingleton(container1.Resolve<IWeightSortingRepository>());
                        services.AddSingleton(container1.Resolve<IWeightRuleRepository>());

                        services.AddSingleton(container1.Resolve<IVolumeSortingRepository>());
                        services.AddSingleton(container1.Resolve<IVolumeRuleRepository>());

                        services.AddSingleton(container1.Resolve<ILogisticsSortingRepository>());
                        services.AddSingleton(container1.Resolve<ILogisticsRuleRepository>());

                        services.AddSingleton(container1.Resolve<IOcrSortingRepository>());
                        services.AddSingleton(container1.Resolve<IOcrRuleRepository>());

                        services.AddSingleton(container1.Resolve<IApiSortingRepository>());
                        services.AddSingleton(container1.Resolve<IApiRuleRepository>());

                        services.AddSingleton(container1.Resolve<ICommunicationConnectionConfigRepository>());
                        services.AddSingleton(container1.Resolve<IDeviceExtensionConfigRepository>());
                        services.AddSingleton(container1.Resolve<IHeartbeatConfigRepository>());
                        services.AddSingleton(container1.Resolve<ISerialPortConfigRepository>());
                        services.AddSingleton(container1.Resolve<ITcpConfigRepository>());
                        services.AddSingleton(container1.Resolve<ITcpConnectionConfigRepository>());
                        //logs
                        services.AddSingleton(container1.Resolve<IAppLogRepository>());
                        services.AddSingleton(container1.Resolve<ICameraLogRepository>());
                        services.AddSingleton(container1.Resolve<ISortingLogRepository>());
                        services.AddSingleton(container1.Resolve<IWeighingLogRepository>());
                        services.AddSingleton(container1.Resolve<IVolumeLogRepository>());
                        services.AddSingleton(container1.Resolve<IApiLogRepository>());
                        services.AddSingleton(container1.Resolve<IOutputLogRepository>());
                        services.AddSingleton(container1.Resolve<IInputLogRepository>());
                        services.AddSingleton(container1.Resolve<IOcrLogRepository>());
                        services.AddSingleton(container1.Resolve<IFtpLogRepository>());
                        services.AddSingleton(container1.Resolve<ICleanupLogRepository>());
                        services.AddSingleton(container1.Resolve<IExceptionLogRepository>());
                        //Api接口注册

                        services.AddSingleton<IDataUploader, DefaultApi>();

                        //OCR

                        services.AddSingleton(container1.Resolve<IOcr>());
                        services.AddSingleton(container1.Resolve<IComputer>());
                        services.AddSingleton(container1.Resolve<IComputerInfoReporter>());
                        services.AddSingleton(container1.Resolve<IFtp>());
                        services.AddSingleton(container1.Resolve<IExcel>());
                        services.AddSingleton(container1.Resolve<ISaveImage>());
                        services.AddSingleton(container1.Resolve<ISpeech>());
                        services.AddSingleton(container1.Resolve<ITcpCommClient>());
                        services.AddSingleton(container1.Resolve<ITcpCommServer>());

                        services.AddSingleton(container1.Resolve<ITcpContentOutput>());
                        services.AddSingleton(container1.Resolve<ITcpVolumeInput>());

                        services.AddSingleton(container1.Resolve<IDynamicScale>());
                        services.AddSingleton(container1.Resolve<IStaticScale>());

                        services.AddSingleton(container1.Resolve<IDeviceService>());
                        services.AddSingleton(container1.Resolve<IImageStorageService>());
                        services.AddSingleton(container1.Resolve<IResultOutputService>());

                        services.AddSingleton(container1.Resolve<IExternalDataService>());
                        services.AddSingleton(container1.Resolve<ICacheCleanupService>());
                        //补注册

                        //注册分拣服务
                        services.AddSingleton(container1.Resolve<ISortingService>());
                        services.AddSingleton(container1.Resolve<ISortingConnectionService>());
                        //云端
                        services.AddSingleton(container1.Resolve<ICloud>());

                        services.AddHostedService<PackageBackgroundService>(); // 注册后组包服务
                        services.AddHostedService<SaveImageBackgroundService>();//注册存图服务
                        services.AddHostedService<SubmitApiBackgroundService>();//提交Api
                        services.AddHostedService<DataProcessingBackgroundService>();//数据处理
                        services.AddHostedService<CleanupService>();//清理
                        services.AddHostedService<ComputerInfoBackgroundService>(); // 注册后台服务
                        services.AddHostedService<SingleInstanceBackgroundService>(); // 注册单开激活服务
                        services.AddHostedService<LogProcessingService>();//日志管理器
                        services.AddHostedService<TimerBackgroundService>();//计时
                        services.AddHostedService<CloudBackgroundService>();//上传云端
                    })
                    .Build();
                _host.Start();
            });
        }

        protected override async void OnExit(ExitEventArgs e) {
            var deviceService = _host?.Services.GetService<IDeviceService>();
            if (deviceService is not null) {
                if (deviceService.RunningStatus) {
                    await deviceService.Stop();
                }
            }

            var sortingService = _host?.Services.GetService<ISortingService>();
            if (sortingService is not null) {
                if (sortingService.RunningStatus) {
                    await sortingService.Stop();
                }
            }
            if (_host is not null) {
                await _host.StopAsync();
                _host.Dispose();
            }

            await Task.Delay(500);
            if (_singleInstanceMutex is not null) {
                _singleInstanceMutex.ReleaseMutex();
                _singleInstanceMutex.Close();
            }

            EventAggregator.Instance.Publish(new AppLogInfoModel {
                CreateTime = DateTime.Now,
                Message = "程序关闭",
                Type = LogType.Information
            });
            base.OnExit(e);
        }

        private void NotifyExistingInstance() {
            try {
                using (var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out)) {
                    pipeClient.Connect(5000); // 连接到已存在的管道
                    using (var sw = new StreamWriter(pipeClient)) {
                        sw.Write("ActivateWindow");
                    }
                }
            }
            catch (TimeoutException) {
                // 如果连接超时，可以处理错误情况
            }
        }

        protected override void ConfigureViewModelLocator() {
            base.ConfigureViewModelLocator();
            //绑定页面
            ViewModelLocationProvider.Register<ExportDialog, ExportDialogViewModel>();
            ViewModelLocationProvider.Register<LoadingDialog, LoadingDialogViewModel>();
            ViewModelLocationProvider.Register<DataTimeEditor, DataTimeEditorViewModel>();
            ViewModelLocationProvider.Register<PackageExitDefinitionEditor, PackageExitDefinitionEditorViewModel>();
            ViewModelLocationProvider.Register<LogisticsCodeRecognitionEditor, LogisticsCodeRecognitionEditorViewModel>();
            ViewModelLocationProvider.Register<SortingInstructionBindingEditor, SortingInstructionBindingEditorViewModel>();
            ViewModelLocationProvider.Register<BarcodeSortingRuleEditor, BarcodeSortingRuleEditorViewModel>();
            ViewModelLocationProvider.Register<WeightSortingRuleEditor, WeightSortingRuleEditorViewModel>();
            ViewModelLocationProvider.Register<VolumeSortingRuleEditor, VolumeSortingRuleEditorViewModel>();
            ViewModelLocationProvider.Register<LogisticsSortingRuleEditor, LogisticsSortingRuleEditorViewModel>();
            ViewModelLocationProvider.Register<OcrSortingRuleEditor, OcrSortingRuleEditorViewModel>();
            ViewModelLocationProvider.Register<ApiSortingRuleEditor, ApiSortingRuleEditorViewModel>();
            ViewModelLocationProvider.Register<CommunicationConnectionConfigEditor, CommunicationConnectionConfigEditorViewModel>();

            ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
            ViewModelLocationProvider.Register<SettingsPage, SettingsViewModel>();
            ViewModelLocationProvider.Register<PluginMarketplacePage, PluginMarketplaceViewModel>();
            ViewModelLocationProvider.Register<HomePage, HomeViewModel>();
            ViewModelLocationProvider.Register<StatusBarPage, StatusBarViewModel>();
            ViewModelLocationProvider.Register<ApiAccessDialog, ApiAccessViewModel>();
            ViewModelLocationProvider.Register<BarCodeDetailsDialog, BarCodeDetailsDialogViewModel>();

            ViewModelLocationProvider.Register<ApiTestDialog, ApiTestViewModel>();
            ViewModelLocationProvider.Register<DataManagementPage, DataManagementViewModel>();
            ViewModelLocationProvider.Register<CameraConfigurationPage, CameraConfigurationViewModel>();
            ViewModelLocationProvider.Register<BarcodeScannerCameraConfigPage, BarcodeScannerCameraConfigViewModel>();
            ViewModelLocationProvider.Register<PanoramaCameraConfigPage, PanoramaCameraConfigViewModel>();
            ViewModelLocationProvider.Register<VolumeCameraConfigPage, VolumeCameraConfigViewModel>();
            ViewModelLocationProvider.Register<CameraFinderPage, CameraFinderViewModel>();

            ViewModelLocationProvider.Register<APISettingsPage, ApiSettingsPageViewModel>();
            ViewModelLocationProvider.Register<SaveImageSettingsPage, SaveImageSettingsPageViewModel>();
            ViewModelLocationProvider.Register<BarcodeFilterSettingsPage, BarcodeFilterSettingsPageViewModel>();
            ViewModelLocationProvider.Register<ResultOutputSettingsPage, ResultOutputSettingsPageViewModel>();
            ViewModelLocationProvider.Register<ContentInputSettingsPage, ContentInputSettingsPageViewModel>();
            ViewModelLocationProvider.Register<CacheClearSettingsPage, CacheClearSettingsPageViewModel>();
            ViewModelLocationProvider.Register<BarcodeFilterSettingsPage, BarcodeFilterSettingsPageViewModel>();
            ViewModelLocationProvider.Register<WeightSettingPages, WeightSettingViewModel>();
            ViewModelLocationProvider.Register<VolumeSettingsPage, VolumeSettingsViewModel>();
            ViewModelLocationProvider.Register<AppSettingsPage, AppSettingsViewModel>();
            ViewModelLocationProvider.Register<LogManagerPage, LogManagerViewModel>();
            ViewModelLocationProvider.Register<VideoCameraSettingsDialog, VideoCameraSettingsViewModel>();
            ViewModelLocationProvider.Register<TriggerModeSelectionPage, TriggerModeSelectionViewModel>();
            ViewModelLocationProvider.Register<ScanCameraSelectionDialog, ScanCameraSelectionDialogViewModel>();
            ViewModelLocationProvider.Register<ResolutionConstraintDialog, ResolutionConstraintViewModel>();
            ViewModelLocationProvider.Register<CloudServicePage, CloudServicePageViewModel>();

            ViewModelLocationProvider.Register<PackageSortingSettingsPage, PackageSortingSettingsViewModel>();
            ViewModelLocationProvider.Register<OcrSettingsPage, OcrSettingsViewModel>();
            ViewModelLocationProvider.Register<WorkflowSettingsPage, WorkflowSettingsViewModel>();

            ViewModelLocationProvider.Register<LogisticsCodeRecognitionPage, LogisticsCodeRecognitionViewModel>();
            ViewModelLocationProvider.Register<PackageExitDefinitionPage, PackageExitDefinitionViewModel>();
            ViewModelLocationProvider.Register<SortingInstructionBindingPage, SortingInstructionBindingViewModel>();
            ViewModelLocationProvider.Register<SortingSchemeSettingsPage, SortingSchemeSettingsViewModel>();
            ViewModelLocationProvider.Register<CommunicationsSettingsPage, CommunicationsSettingsViewModel>();
            ViewModelLocationProvider.Register<SortingMethodPage, SortingMethodViewModel>();
            //分拣模式
            ViewModelLocationProvider.Register<BarcodeSortingPage, BarcodeSortingViewModel>();
            ViewModelLocationProvider.Register<WeightSortingPage, WeightSortingViewModel>();

            ViewModelLocationProvider.Register<VolumeSortingPage, VolumeSortingViewModel>();
            ViewModelLocationProvider.Register<LogisticsSortingPage, LogisticsSortingViewModel>();
            ViewModelLocationProvider.Register<OcrSortingPage, OcrSortingViewModel>();
            ViewModelLocationProvider.Register<ApiResponseSortingPage, ApiResponseSortingViewModel>();
            ViewModelLocationProvider.Register<CombinedWorkflowSortingPage, CombinedWorkflowSortingViewModel>();
            //程序设置
            ViewModelLocationProvider.Register<GridSettingsPage, GridSettingsViewModel>();
            ViewModelLocationProvider.Register<OtherSettingsPage, OtherSettingsViewModel>();
            //日志
            ViewModelLocationProvider.Register<AppLogPage, AppLogPageViewModel>();
            ViewModelLocationProvider.Register<CameraLogPage, CameraLogPageViewModel>();
            ViewModelLocationProvider.Register<SortingLogPage, SortingLogPageViewModel>();
            ViewModelLocationProvider.Register<WeighingLogPage, WeighingLogPageViewModel>();
            ViewModelLocationProvider.Register<VolumeLogPage, VolumeLogPageViewModel>();
            ViewModelLocationProvider.Register<APILogPage, ApiLogPageViewModel>();
            ViewModelLocationProvider.Register<OutputLogPage, OutputLogPageViewModel>();
            ViewModelLocationProvider.Register<FTPLogPage, FtpLogPageViewModel>();
            ViewModelLocationProvider.Register<ExceptionLogPage, ExceptionLogPageViewModel>();
            //云端服务
            ViewModelLocationProvider.Register<CloudDataPage, CloudDataPageViewModel>();
            ViewModelLocationProvider.Register<CloudVideoPage, CloudVideoSettingsPageViewModel>();

            //接口
            ViewModelLocationProvider.Register<DefaultApiPage, DefaultApiPageViewModel>();
            ViewModelLocationProvider.Register<SzjyApiPage, SzjyApiPageViewModel>();
            ViewModelLocationProvider.Register<WdtFlagshipApiPage, WdtFlagshipApiPageViewModel>();
            ViewModelLocationProvider.Register<WdtWmsApiPage, WdtWmsApiPageViewModel>();
            //实时日志
            ViewModelLocationProvider.Register<RealTimeLogPage, RealTimeLogViewModel>();
            //其他插件
            {
                ViewModelLocationProvider.Register<SunnenInputBarcodeControl, SunnenInputBarcodeViewModel>();
            }
        }
    }
}