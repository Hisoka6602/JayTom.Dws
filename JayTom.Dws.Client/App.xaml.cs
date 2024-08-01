using DryIoc;
using System;
using Example;
using Prism.Ioc;
using System.IO;
using Prism.Mvvm;
using Prism.DryIoc;
using System.Windows;
using JayTom.Dws.Ocr;
using JayTom.Dws.Nvr;
using Newtonsoft.Json;
using System.IO.Pipes;
using System.Net.Http;
using System.IO.Ports;
using System.Threading;
using JayTom.Dws.Camera;
using JayTom.Dws.Plugin;
using System.Diagnostics;
using JayTom.Dws.Nvr.Nvr;
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
using JayTom.Dws.Interface.License;
using JayTom.Dws.Client.Views.Pages;
using Microsoft.EntityFrameworkCore;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.Views.Editors;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;
using JayTom.Dws.Camera.BarCodeReader;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Infrastructure.Service;
using JayTom.Dws.Client.ViewModels.Pages;
using Microsoft.Extensions.Configuration;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Plugin.Scale.StaticScale;
using JayTom.Dws.Client.ViewModels.Editors;
using JayTom.Dws.Plugin.Scale.DynamicScale;
using JayTom.Dws.Domain.Repository.LocalLog;
using JayTom.Dws.Interface.Cloud.CloudVideo;
using JayTom.Dws.Client.Service.TestService;
using JayTom.Dws.Client.Service.ResultOutput;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Service.CacheCleanup;
using JayTom.Dws.Client.Service.SyncSettings;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.Client.Service.ImageService;
using Microsoft.Extensions.DependencyInjection;
using JayTom.Dws.Plugin.Device.GrayscaleDevice;
using JayTom.Dws.Client.Views.Pages.Preferences;
using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Client.Views.Editors.CloudService;
using JayTom.Dws.Client.Service.ProcessingServices;
using JayTom.Dws.Client.Service.ExternalDataService;
using JayTom.Dws.Infrastructure.Repository.LocalLog;
using DryIoc.Microsoft.DependencyInjection.Extension;
using JayTom.Dws.Client.ViewModels.Pages.Preferences;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.Client.Service.DefaultConfiguration;
using JayTom.Dws.Camera.Cameras.SmartCamera.Hikvision;
using JayTom.Dws.Client.ViewModels.Editors.CloudService;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Client.Views.Dialog.CameraConfiguration;
using JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.Views;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Client.Views.Pages.Preferences.LogsViews;
using JayTom.Dws.Domain.Repository.LocalConf.IpcNvrConfig;
using JayTom.Dws.Client.Views.Editors.CameraConfiguration;
using JayTom.Dws.Client.Views.Pages.Preferences.AppSettings;
using JayTom.Dws.Client.Views.Pages.Preferences.CloudService;
using JayTom.Dws.Client.Service.Sorting.Communication.TcpComm;
using JayTom.Dws.Client.ViewModels.Dialog.CameraConfiguration;
using JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.ViewModels;
using JayTom.Dws.Client.ViewModels.Editors.CameraConfiguration;
using JayTom.Dws.Client.Service.Sorting.Communication.SerialComm;
using JayTom.Dws.Client.Views.Pages.Preferences.ApiConfiguration;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.AppSettings;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.CloudService;
using JayTom.Dws.Infrastructure.SignalR.CloudApi.ClientMessageHub;
using JayTom.Dws.Infrastructure.Repository.LocalConf.IpcNvrConfig;
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
            var rules = containerRegistry.GetContainer().Rules;
            rules.WithoutThrowOnRegisteringDisposableTransient();

            //注册窗口
            containerRegistry.RegisterDialog<ApiAccessDialog>();
            containerRegistry.RegisterDialog<ApiTestDialog>();
            containerRegistry.RegisterDialog<PackageDetailsDialog>();

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
                containerRegistry.RegisterForNavigation<CreatePackageSettingsPage>();
                //LogManagerPage
                //相机
                containerRegistry.RegisterForNavigation<BarcodeScannerCameraConfigPage>();
                containerRegistry.RegisterForNavigation<CameraFinderPage>();
                containerRegistry.RegisterForNavigation<PanoramaCameraConfigPage>();
                containerRegistry.RegisterForNavigation<VolumeCameraConfigPage>();
                containerRegistry.RegisterForNavigation<UsbCameraSettingsPage>();
                containerRegistry.RegisterForNavigation<AlgorithmSettingsPage>();
                containerRegistry.RegisterForNavigation<NVRIPCDeviceManagementPage>();
                //分拣设置
                containerRegistry.RegisterForNavigation<LogisticsCodeRecognitionPage>();
                containerRegistry.RegisterForNavigation<PackageExitDefinitionPage>();
                containerRegistry.RegisterForNavigation<SortingInstructionBindingPage>();
                containerRegistry.RegisterForNavigation<SortingSchemeSettingsPage>();
                containerRegistry.RegisterForNavigation<CommunicationsSettingsPage>();
                containerRegistry.RegisterForNavigation<SortingMethodPage>();
                containerRegistry.RegisterForNavigation<PackageExitLockSettingsPage>();
                containerRegistry.RegisterForNavigation<StackedPackageDetectionSettingsPage>();
                containerRegistry.RegisterForNavigation<SupplyCounterSettingsPage>();
                containerRegistry.RegisterForNavigation<GrayscaleDeviceSettingsPage>();
                //程序设置
                containerRegistry.RegisterForNavigation<GridSettingsPage>();
                containerRegistry.RegisterForNavigation<OtherSettingsPage>();
                containerRegistry.RegisterForNavigation<LicensePage>();
                containerRegistry.RegisterForNavigation<SyncSettingsPage>();
                containerRegistry.RegisterForNavigation<PassWordSettingsPage>();
                //云端服务
                containerRegistry.RegisterForNavigation<CloudDataPage>();
                containerRegistry.RegisterForNavigation<CloudVideoPage>();
                containerRegistry.RegisterForNavigation<NetworkVideoRecorderPage>();
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
                // 注册 IConfiguration
                services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build());
                services.AddSingleton<IPackageRepository, PackageRepository>();
                services.AddSingleton<IBarCodeRepository, BarCodeRepository>();
                services.AddSingleton<ISoundRepository, SoundRepository>();
                services.AddSingleton<IVolumeRepository, VolumeRepository>();
                services.AddSingleton<IWeightRepository, WeightRepository>();
                services.AddSingleton<IUploadRepository, UploadRepository>();
                services.AddSingleton<ISortingRepository, SortingRepository>();
                services.AddSingleton<IOcrRepository, OcrRepository>();
                services.AddSingleton<IImageRepository, ImageRepository>();
                services.AddSingleton<ICloudVideoUploadRepository, CloudVideoUploadRepository>();
                services.AddSingleton<IExitInfoRepository, ExitInfoRepository>();

                //config
                services.AddSingleton<IBarcodeScannerCameraConfigRepository, BarcodeScannerCameraConfigRepository>();
                services.AddSingleton<IPanoramaCameraConfigRepository, PanoramaCameraConfigRepository>();
                services.AddSingleton<IVolumeCameraConfigRepository, VolumeCameraConfigRepository>();

                services.AddSingleton<IUsbCameraConfigRepository, UsbCameraConfigRepository>();
                services.AddSingleton<IConfigRepository, ConfigRepository>();
                services.AddSingleton<ILogisticsCodeRecognitionRepository, LogisticsCodeRecognitionRepository>();
                services.AddSingleton<IPackageExitDefinitionRepository, PackageExitDefinitionRepository>();
                services.AddSingleton<ISortingInstructionBindingRepository, SortingInstructionBindingRepository>();
                services.AddSingleton<ILogisticsRegexRepository, LogisticsRegexRepository>();
                services.AddSingleton<ISortingInstructionRepository, SortingInstructionRepository>();
                services.AddSingleton<IPackageExitLockBindingRepository, PackageExitLockBindingRepository>();

                services.AddSingleton<IBarCodeSortingRepository, BarCodeSortingRepository>();
                services.AddSingleton<IBarCodeRegexRepository, BarCodeRegexRepository>();

                services.AddSingleton<IWeightSortingRepository, WeightSortingRepository>();
                services.AddSingleton<IWeightRuleRepository, WeightRuleRepository>();

                services.AddSingleton<IVolumeSortingRepository, VolumeSortingRepository>();
                services.AddSingleton<IVolumeRuleRepository, VolumeRuleRepository>();

                services.AddSingleton<ILogisticsSortingRepository, LogisticsSortingRepository>();
                services.AddSingleton<ILogisticsRuleRepository, LogisticsRuleRepository>();

                services.AddSingleton<IOcrSortingRepository, OcrSortingRepository>();
                services.AddSingleton<IOcrRuleRepository, OcrRuleRepository>();

                services.AddSingleton<IApiSortingRepository, ApiSortingRepository>();
                services.AddSingleton<IApiRuleRepository, ApiRuleRepository>();

                services.AddSingleton<ICommunicationConnectionConfigRepository, CommunicationConnectionConfigRepository>();
                services.AddSingleton<IDeviceExtensionConfigRepository, DeviceExtensionConfigRepository>();
                services.AddSingleton<IHeartbeatConfigRepository, HeartbeatConfigRepository>();
                services.AddSingleton<ISerialPortConfigRepository, SerialPortConfigRepository>();
                services.AddSingleton<ITcpConfigRepository, TcpConfigRepository>();
                services.AddSingleton<ITcpConnectionConfigRepository, TcpConnectionConfigRepository>();
                services.AddSingleton<INvrCameraBindingRepository, NvrCameraBindingRepository>();

                services.AddSingleton<IIpcNvrConfigRepository, IpcNvrConfigRepository>();

                //logs
                services.AddSingleton<IAppLogRepository, AppLogRepository>();
                services.AddSingleton<ICameraLogRepository, CameraLogRepository>();
                services.AddSingleton<ISortingLogRepository, SortingLogRepository>();
                services.AddSingleton<IWeighingLogRepository, WeighingLogRepository>();
                services.AddSingleton<IVolumeLogRepository, VolumeLogRepository>();
                services.AddSingleton<IApiLogRepository, ApiLogRepository>();
                services.AddSingleton<IOutputLogRepository, OutputLogRepository>();
                services.AddSingleton<IInputLogRepository, InputLogRepository>();
                services.AddSingleton<IOcrLogRepository, OcrLogRepository>();
                services.AddSingleton<IFtpLogRepository, FtpLogRepository>();
                services.AddSingleton<ICleanupLogRepository, CleanupLogRepository>();
                services.AddSingleton<IExceptionLogRepository, ExceptionLogRepository>();
                //插件注册
                services.AddSingleton<IExcel, NpoiExport>();
                services.AddSingleton<IFtp, FluentFtpClient>();
                services.AddSingleton<ISaveImage, SaveImage>();
                services.AddSingleton<ISpeech, Speech>();
                services.AddSingleton<ITcpCommClient, TouchSocketTcpClient>();
                services.AddSingleton<ITcpCommServer, TouchSocketTcpServer>();
                services.AddSingleton<ITcpContentOutput>(provider => new TcpContentOutput(new TouchSocketTcpClient(), new TouchSocketTcpServer()));
                services.AddSingleton<ITcpVolumeInput>(provider => new TcpVolumeInput(new TouchSocketTcpClient(), new TouchSocketTcpServer()));
                services.AddSingleton<ITcpContentInput>(provider => new TcpContentInput(new TouchSocketTcpClient(), new TouchSocketTcpServer()));
                services.AddSingleton<ISortingSerialPort>(serialPort => new SortingSerialPort(new SerialPort()));
                services.AddSingleton<ISortingTcp>(provider => new SortingTcp(new TouchSocketTcpClient(), new TouchSocketTcpServer()));
                //叠包监控通讯注册
                services.AddSingleton<IPackageDetectionSerialPort>(serialPort => new PackageDetectionSerialPort(new SerialPort()));
                services.AddSingleton<IPackageDetectionTcp>(provider => new PackageDetectionTcp(new TouchSocketTcpClient(), new TouchSocketTcpServer()));
                //效验注册
                services.AddSingleton<INetworkTime, NetworkTime>();
                services.AddSingleton<ICertificateValidationService, CertificateValidationService>();
                //电脑注册
                services.AddSingleton<IComputer, Infrastructure.IComputer.Computer>();
                //电脑信息上报
                services.AddSingleton<IComputerInfoReporter, ComputerInfoReporter>();
                //写默认配置
                services.AddSingleton<IDefaultConfigurationService, DefaultConfigurationService>();
                //设备注册
                services.AddSingleton<ICamera, HikvisionSmartCamera>();
                //Ocr
                services.AddSingleton<IOcr, ExpressBillOcr>();
                //读码器
                //services.AddSingleton<IBarCodeReader, DynamsoftBarCodeReader>();
                //磅秤
                services.AddSingleton<IDynamicScale, DefaultDynamicScale>();
                services.AddSingleton<IStaticScale, DefaultStaticScale>();
                services.AddSingleton<IDeviceService, DefaultDeviceService>();
                services.AddSingleton<IImageStorageService, DefaultImageStorageService>();
                services.AddSingleton<IResultOutputService, DefaultResultOutputService>();
                services.AddSingleton<IExternalDataService, ExternalDataService>();
                //基础服务注册
                services.AddSingleton<ICacheCleanupService, CacheCleanupService>();
                //分拣注册
                services.AddSingleton<ISortingService, DefaultSortingService>();
                //锁格监控注册
                services.AddSingleton<IExitMonitor, DefaultExitMonitor>();
                //叠包监控注册
                services.AddSingleton<IStackedPackageService, DefaultStackedPackageService>();
                services.AddSingleton<ISortingConnectionService, DefaultSortingConnectionService>();
                //灰度仪服务注册
                services.AddSingleton<IGrayscaleService, DefaultGrayscaleService>();
                services.AddSingleton<IGrayscaleDevice>(provider => new GwGrayscaleDevice(new TouchSocketTcpClient(), new TouchSocketTcpServer()));

                //云视频云端
                services.AddSingleton<ICloud, CloudVideoUploadApi>();
                //Nvr
                services.AddSingleton<INvrManager, DaHuaNvr>();
                //授权接口
                services.AddSingleton<IClientLicenseApi, DefaultClientLicenseApi>();
                //SignalR
                services.AddSingleton<ICloudApiClientMessageHub, CloudApiClientMessageHub>();
                //同步配置
                services.AddSingleton<ISyncSettingsService, SyncSettingsService>();
                //把后台注册服务写在这里
                services.AddHostedService<LianJiangPostPackageBackgroundService>(); // 注册后组包服务
                services.AddHostedService<SaveImageBackgroundService>();//注册存图服务
                services.AddHostedService<SubmitApiBackgroundService>();//提交Api
                services.AddHostedService<DataProcessingBackgroundService>();//数据处理
                services.AddHostedService<CleanupService>();//清理
                services.AddHostedService<ComputerInfoBackgroundService>(); // 注册后台服务
                services.AddHostedService<SingleInstanceBackgroundService>(); // 注册单开激活服务
                services.AddHostedService<LogProcessingService>();//日志管理器
                services.AddHostedService<TimerBackgroundService>();//计时
                services.AddHostedService<CloudBackgroundService>();//上传云端
                //services.AddHostedService<PackageAggregationService>();//集包服务
                services.AddHostedService<PackageExitUpdateBackgroundService>();//格口更新
            });
        }

        protected override Window CreateShell() {
            return Container.Resolve<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e) {
            NLog.LogManager.GetCurrentClassLogger().Error($"OnStartup开始");
            _singleInstanceMutex = new Mutex(true, "Dws.Client", out var createdNew);
            if (!createdNew) {
                // 另一个实例已经在运行，尝试激活它的窗口
                NotifyExistingInstance();
                NLog.LogManager.GetCurrentClassLogger().Error("阻止多开");
                Environment.Exit(0);
            }
            ThreadPool.SetMinThreads(100, 200);

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
            TaskScheduler.UnobservedTaskException += (sender, args) => {
                //异常触发
                NLog.LogManager.GetCurrentClassLogger().Error($"{JsonConvert.SerializeObject(args.Exception)}");
                EventAggregator.Instance.Publish(new AppLogInfoModel {
                    CreateTime = DateTime.Now,
                    Message = args.Exception.Message,
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

            NLog.LogManager.GetCurrentClassLogger().Error($"OnStartup结束");
        }

        protected override async void OnExit(ExitEventArgs e) {
            EventAggregator.Instance.Publish(new AppLogInfoModel {
                CreateTime = DateTime.Now,
                Message = "程序关闭",
                Type = LogType.Information
            });
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
            /*if (_host is not null) {
                await _host.StopAsync();
                _host.Dispose();
            }*/
            if (_singleInstanceMutex is not null) {
                _singleInstanceMutex.ReleaseMutex();
                _singleInstanceMutex.Close();
            }

            var hostedServices = Container.GetContainer().GetServices<IHostedService>();

            Parallel.ForEach(hostedServices, service => {
                service.StopAsync(default);
            });
            /*foreach (var service in hostedServices) {
                service.StopAsync(default).Wait();
            }*/

            await Task.Delay(1000);

            GC.Collect();
            base.OnExit(e);
        }

        private void NotifyExistingInstance() {
            try {
                using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                pipeClient.Connect(5000); // 连接到已存在的管道
                using var sw = new StreamWriter(pipeClient);
                sw.Write("ActivateWindow");
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
            ViewModelLocationProvider.Register<BulkDeleteAccessDialog, BulkDeleteAccessViewModel>();
            ViewModelLocationProvider.Register<PackageExitLockEditor, PackageExitLockEditorViewModel>();
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
            ViewModelLocationProvider.Register<RegularExpressionEditor, RegularExpressionEditorViewModel>();

            //Ipc/Nvr编辑
            ViewModelLocationProvider.Register<NvrIpcDeviceEditor, NvrIpcDeviceEditorViewModel>();
            ViewModelLocationProvider.Register<NvrCameraMappingEditor, NvrCameraMappingEditorViewModel>();
            ViewModelLocationProvider.Register<NvrBindingEditor, NvrBindingEditorViewModel>();

            ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
            ViewModelLocationProvider.Register<SettingsPage, SettingsViewModel>();
            ViewModelLocationProvider.Register<PluginMarketplacePage, PluginMarketplaceViewModel>();
            ViewModelLocationProvider.Register<HomePage, HomeViewModel>();
            ViewModelLocationProvider.Register<StatusBarPage, StatusBarViewModel>();
            ViewModelLocationProvider.Register<ApiAccessDialog, ApiAccessViewModel>();
            ViewModelLocationProvider.Register<PackageDetailsDialog, PackageDetailsDialogViewModel>();
            ViewModelLocationProvider.Register<IpcPreviewDialog, IpcPreviewViewModel>();

            ViewModelLocationProvider.Register<ApiTestDialog, ApiTestViewModel>();
            ViewModelLocationProvider.Register<DataManagementPage, DataManagementViewModel>();
            ViewModelLocationProvider.Register<CameraConfigurationPage, CameraConfigurationViewModel>();
            ViewModelLocationProvider.Register<BarcodeScannerCameraConfigPage, BarcodeScannerCameraConfigViewModel>();
            ViewModelLocationProvider.Register<PanoramaCameraConfigPage, PanoramaCameraConfigViewModel>();
            ViewModelLocationProvider.Register<VolumeCameraConfigPage, VolumeCameraConfigViewModel>();
            ViewModelLocationProvider.Register<CameraFinderPage, CameraFinderViewModel>();
            ViewModelLocationProvider.Register<UsbCameraSettingsPage, UsbCameraSettingsViewModel>();
            ViewModelLocationProvider.Register<AlgorithmSettingsPage, AlgorithmSettingsViewModel>();
            ViewModelLocationProvider.Register<NVRIPCDeviceManagementPage, NvrIpcDeviceManagementViewModel>();

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
            ViewModelLocationProvider.Register<PasswordValidationDialog, PasswordValidationDialogViewModel>();

            ViewModelLocationProvider.Register<NetworkVideoRecorderPage, NetworkVideoRecorderPageViewModel>();
            ViewModelLocationProvider.Register<PackageSortingSettingsPage, PackageSortingSettingsViewModel>();
            ViewModelLocationProvider.Register<OcrSettingsPage, OcrSettingsViewModel>();
            ViewModelLocationProvider.Register<WorkflowSettingsPage, WorkflowSettingsViewModel>();

            ViewModelLocationProvider.Register<LogisticsCodeRecognitionPage, LogisticsCodeRecognitionViewModel>();
            ViewModelLocationProvider.Register<PackageExitDefinitionPage, PackageExitDefinitionViewModel>();
            ViewModelLocationProvider.Register<SortingInstructionBindingPage, SortingInstructionBindingViewModel>();
            ViewModelLocationProvider.Register<SortingSchemeSettingsPage, SortingSchemeSettingsViewModel>();
            ViewModelLocationProvider.Register<CommunicationsSettingsPage, CommunicationsSettingsViewModel>();
            ViewModelLocationProvider.Register<SortingMethodPage, SortingMethodViewModel>();
            ViewModelLocationProvider.Register<PackageExitLockSettingsPage, PackageExitLockSettingsViewModel>();
            ViewModelLocationProvider.Register<StackedPackageDetectionSettingsPage, StackedPackageDetectionSettingsViewModel>();
            ViewModelLocationProvider.Register<SupplyCounterSettingsPage, SupplyCounterSettingsViewModel>();
            ViewModelLocationProvider.Register<GrayscaleDeviceSettingsPage, GrayscaleDeviceSettingsViewModel>();
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
            ViewModelLocationProvider.Register<LicensePage, LicensePageViewModel>();
            ViewModelLocationProvider.Register<SyncSettingsPage, SyncSettingsViewModel>();
            ViewModelLocationProvider.Register<PassWordSettingsPage, PassWordSettingsViewModel>();

            //组包设置
            ViewModelLocationProvider.Register<CreatePackageSettingsPage, CreatePackageSettingsViewModel>();
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
            //Nvr绑定页面
            ViewModelLocationProvider.Register<NvrCameraBindingEditor, NvrCameraBindingEditorViewModel>();
            //接口
            ViewModelLocationProvider.Register<DefaultApiPage, DefaultApiPageViewModel>();
            ViewModelLocationProvider.Register<SzjyApiPage, SzjyApiPageViewModel>();
            ViewModelLocationProvider.Register<WdtFlagshipApiPage, WdtFlagshipApiPageViewModel>();
            ViewModelLocationProvider.Register<WdtWmsApiPage, WdtWmsApiPageViewModel>();
            ViewModelLocationProvider.Register<JtExpressApiPage, JtExpressApiPageViewModel>();
            ViewModelLocationProvider.Register<RoutDataApiPage, RoutDataApiViewPageModel>();
            ViewModelLocationProvider.Register<CaiNiaoApiPage, CaiNiaoApiPageViewModel>();
            ViewModelLocationProvider.Register<EshippingitApiPage, EshippingitApiPageViewModel>();
            //实时日志
            //实时日志
            ViewModelLocationProvider.Register<RealTimeLogPage, RealTimeLogViewModel>();
            //其他插件
            {
                ViewModelLocationProvider.Register<SunnenInputBarcodeControl, SunnenInputBarcodeViewModel>();
            }
        }

        protected override async void OnInitialized() {
            await Task.Yield();
            base.OnInitialized();
            // 获取 IServiceProvider
            var serviceProvider = Container.Resolve<IServiceProvider>();

            // 启动 PackageAggregationService
            var hostedServices = serviceProvider.GetServices<IHostedService>();

            /*
            Parallel.ForEach(hostedServices, async service => {
                await service.StartAsync(default);
            });*/

            foreach (var service in hostedServices) {
                var serviceName = service.GetType().Name;
                NLog.LogManager.GetCurrentClassLogger().Error($"服务名: {serviceName}");
                await service.StartAsync(default);
            }
            NLog.LogManager.GetCurrentClassLogger().Error($"全部服务启动完成");
        }
    }
}