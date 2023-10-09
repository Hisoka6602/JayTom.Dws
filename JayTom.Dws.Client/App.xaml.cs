using System;
using DryIoc;
using Prism.Ioc;
using Prism.Mvvm;
using System.Data;
using System.Linq;
using Prism.DryIoc;
using System.Windows;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Plugin;
using JayTom.Dws.Camera;
using System.Configuration;
using System.Windows.Media;
using JayTom.Dws.Interface;
using System.Globalization;
using JayTom.Dws.Plugin.Ftp;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Windows.Interop;
using JayTom.Dws.Client.Views;
using JayTom.Dws.Plugin.Excel;
using System.Windows.Threading;
using JayTom.Dws.Plugin.Speech;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Infrastructure;
using JayTom.Dws.Client.Service;
using System.Collections.Generic;
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
using JayTom.Dws.Infrastructure.Service;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Client.ViewModels.Pages;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Plugin.Scale.StaticScale;
using JayTom.Dws.Client.ViewModels.Editors;
using JayTom.Dws.Plugin.Scale.DynamicScale;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Service.ImageStorage;
using JayTom.Dws.Client.Service.ResultOutput;
using JayTom.Dws.Domain.Service.CacheCleanup;
using Microsoft.Extensions.DependencyInjection;
using JayTom.Dws.Client.Views.Pages.Preferences;
using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Client.Service.ExternalDataService;
using JayTom.Dws.Client.ViewModels.Pages.Preferences;
using DryIoc.Microsoft.DependencyInjection.Extension;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.Camera.Cameras.SmartCamera.Hikvision;
using JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.Views;
using JayTom.Dws.Client.Service.Sorting.Communication.TcpComm;
using JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.ViewModels;
using JayTom.Dws.Client.Views.Pages.Preferences.ApiConfiguration;
using JayTom.Dws.Client.Service.Sorting.Communication.SerialComm;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration;
using JayTom.Dws.Client.Service.ResultOutput.Communication.TcpComm;
using JayTom.Dws.Client.Views.Pages.Preferences.CameraConfiguration;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration;
using JayTom.Dws.Client.Service.ExternalDataService.Communication.TcpComm;
using JayTom.Dws.Client.Views.Pages.Preferences.PackageSortingConfiguration;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration;

namespace JayTom.Dws.Client {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication {
        private IHost? _host;

        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            //注册窗口
            containerRegistry.RegisterDialog<ApiAccessDialog>();
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
            }
            //其他注册
            containerRegistry.GetContainer().RegisterServices(services => {
                services.AddPooledDbContextFactory<SqliteContext>(options => options.UseSqlite(
                    $"Data Source={System.AppDomain.CurrentDomain.BaseDirectory}Data.db",
                    builder => {
                        builder.CommandTimeout(100); //180秒超时
                        builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    }).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking), 300);
                //配置内存缓存
                services.AddMemoryCache();
                //本地数据表注册
                services.AddScoped<IBarcodeScannerCameraConfigRepository, BarcodeScannerCameraConfigRepository>();
                services.AddScoped<IPanoramaCameraConfigRepository, PanoramaCameraConfigRepository>();
                services.AddScoped<IVolumeCameraConfigRepository, VolumeCameraConfigRepository>();
                services.AddScoped<IBarCodeRepository, BarCodeRepository>();
                services.AddScoped<ISoundRepository, SoundRepository>();
                services.AddScoped<IConfigRepository, ConfigRepository>();
                services.AddScoped<IPanoramaImageRepository, PanoramaImageRepository>();

                services.AddScoped<ILogisticsCodeRecognitionRepository, LogisticsCodeRecognitionRepository>();
                services.AddScoped<IPackageExitDefinitionRepository, PackageExitDefinitionRepository>();
                services.AddScoped<ISortingInstructionBindingRepository, SortingInstructionBindingRepository>();
                services.AddScoped<ILogisticsRegexRepository, LogisticsRegexRepository>();
                services.AddScoped<ISortingInstructionRepository, SortingInstructionRepository>();
                /*services.AddScoped<IConfigRepository, ConfigRepository>();

                //服务注册
                services.AddScoped<IBarcodeScannerService, BarcodeScannerService>();
                //插件注册
                services.AddScoped<ISpeech, Speech>();
                services.AddScoped<IExcel, NpoiExport>();
                //相机
                services.AddScoped<I3DCamera, Percipio3DCamera>();*/
                //插件注册
                services.AddScoped<IExcel, NpoiExport>();
                services.AddScoped<IFtp, FluentFtpClient>();
                services.AddScoped<ISaveImage, SaveImage>();
                services.AddScoped<ISpeech, Speech>();
                services.AddScoped<ITcpCommClient, TouchSocketTcpClient>();
                services.AddScoped<ITcpCommServer, TouchSocketTcpServer>();
                services.AddScoped<ITcpContentOutput, TcpContentOutput>();
                services.AddScoped<ITcpVolumeInput, TcpVolumeInput>();
                services.AddScoped<ISortingSerialPort, SortingSerialPort>();
                services.AddScoped<ISortingTcp, SortingTcp>();
                //电脑注册
                services.AddScoped<IComputer, Computer>();
                //电脑信息上报
                services.AddScoped<IComputerInfoReporter, ComputerInfoReporter>();
                //设备注册
                services.AddScoped<ICamera, HikvisionSmartCamera>();
                //磅秤
                services.AddScoped<IDynamicScale, DefaultDynamicScale>();
                services.AddScoped<IStaticScale, DefaultStaticScale>();

                services.AddScoped<IDeviceService, DefaultDeviceService>();
                services.AddScoped<IImageStorageService, DefaultImageStorageService>();
                services.AddScoped<IResultOutputService, DefaultResultOutputService>();
                services.AddScoped<IExternalDataService, ExternalDataService>();
                //基础服务注册
                services.AddScoped<ICacheCleanupService, CacheCleanupService>();
                //分拣注册 DefaultSortingService
                services.AddScoped<ISortingService, DefaultSortingService>();
            });
        }

        protected override Window CreateShell() {
            return Container.Resolve<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e) {
            ThreadPool.SetMinThreads(300, 200);
            this.DispatcherUnhandledException += delegate (object sender, DispatcherUnhandledExceptionEventArgs args) {
                //异常触发
                NLog.LogManager.GetCurrentClassLogger().Error($"{JsonConvert.SerializeObject(args.Exception)}");
            };
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args) {
                //异常触发
                NLog.LogManager.GetCurrentClassLogger().Error($"{JsonConvert.SerializeObject(args.ExceptionObject)}");
            };

            base.OnStartup(e);

            //加载语言

            var container = Container.GetContainer();

            var configRepository = container.Resolve<IConfigRepository>();
            if (configRepository is not null) {
                var configInfoModel = configRepository.FirstOrDefault(f =>
                    f.ConfigName.Equals("Language")).GetAwaiter().GetResult();
                if (configInfoModel is not null) {
                    var culture = new CultureInfo(configInfoModel.Value);
                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = culture;
                }
            }

            // 创建主机并注册后台服务
            Task.Run(() => {
                // 启用硬件加速
                RenderOptions.ProcessRenderMode = RenderMode.Default;
                var container = Container.GetContainer();
                _host = Host.CreateDefaultBuilder()
                    .ConfigureServices((hostContext, services) => {
                        services.AddHttpClient("INSURANCE", httpClient => {
                            // httpClient.Timeout = TimeSpan.FromSeconds(10);
                        }).ConfigurePrimaryHttpMessageHandler(() => {
                            var handler = new HttpClientHandler() {
                                UseDefaultCredentials = true,
                                MaxConnectionsPerServer = 1000,
                                ServerCertificateCustomValidationCallback = (m, c, ch, _) => true,
                                //UseProxy = false
                            };

                            return handler;
                        });
                        //Api接口注册

                        services.AddSingleton<IDataUploader, DefaultApi>();

                        /*services.AddSingleton<IDataUploader, WeciMexicoDvApi>();
                        services.AddSingleton<ITcpCommunication, TcpCommunication>();
                        services.AddSingleton(container.Resolve<IBarcodeScannerService>());*/

                        services.AddSingleton(container.Resolve<IComputer>());
                        services.AddSingleton(container.Resolve<IComputerInfoReporter>());
                        services.AddSingleton(container.Resolve<IFtp>());
                        services.AddSingleton(container.Resolve<ISaveImage>());
                        services.AddSingleton(container.Resolve<ISpeech>());
                        services.AddSingleton(container.Resolve<ITcpCommClient>());
                        services.AddSingleton(container.Resolve<ITcpCommServer>());

                        services.AddSingleton(container.Resolve<ITcpContentOutput>());
                        services.AddSingleton(container.Resolve<ITcpVolumeInput>());

                        services.AddSingleton(container.Resolve<IDynamicScale>());
                        services.AddSingleton(container.Resolve<IStaticScale>());

                        services.AddSingleton(container.Resolve<IDeviceService>());
                        services.AddSingleton(container.Resolve<IImageStorageService>());
                        services.AddSingleton(container.Resolve<IResultOutputService>());
                        services.AddSingleton(container.Resolve<IBarCodeRepository>());
                        services.AddSingleton(container.Resolve<IBarcodeScannerCameraConfigRepository>());
                        services.AddSingleton(container.Resolve<IPanoramaCameraConfigRepository>());
                        services.AddSingleton(container.Resolve<IVolumeCameraConfigRepository>());
                        services.AddSingleton(container.Resolve<ISoundRepository>());
                        services.AddSingleton(container.Resolve<IConfigRepository>());
                        services.AddSingleton(container.Resolve<IPanoramaImageRepository>());

                        services.AddSingleton(container.Resolve<ILogisticsCodeRecognitionRepository>());
                        services.AddSingleton(container.Resolve<IPackageExitDefinitionRepository>());
                        services.AddSingleton(container.Resolve<ISortingInstructionBindingRepository>());
                        services.AddSingleton(container.Resolve<ILogisticsRegexRepository>());

                        services.AddSingleton(container.Resolve<IExternalDataService>());
                        services.AddSingleton(container.Resolve<ICacheCleanupService>());
                        //补注册

                        services.AddHostedService<ComputerInfoBackgroundService>(); // 注册后台服务
                        services.AddHostedService<ScanProcessBackgroundService>(); // 注册后扫码过程服务
                        services.AddHostedService<SaveImageBackgroundService>();//注册存图服务
                        services.AddHostedService<SubmitApiBackgroundService>();//提交Api
                        services.AddHostedService<CleanupService>();//清理
                        services.AddHostedService<DataProcessingBackgroundService>();//数据处理
                    })
                    .Build();
                _host.Start();
            });
        }

        protected override async void OnExit(ExitEventArgs e) {
            if (_host is not null) {
                await _host.StopAsync();
                _host.Dispose();
            }
            await Task.Delay(500);
            base.OnExit(e);
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

            ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
            ViewModelLocationProvider.Register<SettingsPage, SettingsViewModel>();
            ViewModelLocationProvider.Register<PluginMarketplacePage, PluginMarketplaceViewModel>();
            ViewModelLocationProvider.Register<HomePage, HomeViewModel>();
            ViewModelLocationProvider.Register<StatusBarPage, StatusBarViewModel>();
            ViewModelLocationProvider.Register<ApiAccessDialog, ApiAccessViewModel>();
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
            ViewModelLocationProvider.Register<LogManagerPage, LogManagerViewModel>();
            ViewModelLocationProvider.Register<VideoCameraSettingsDialog, VideoCameraSettingsViewModel>();
            ViewModelLocationProvider.Register<TriggerModeSelectionPage, TriggerModeSelectionViewModel>();
            ViewModelLocationProvider.Register<ResolutionConstraintDialog, ResolutionConstraintViewModel>();

            ViewModelLocationProvider.Register<PackageSortingSettingsPage, PackageSortingSettingsViewModel>();
            ViewModelLocationProvider.Register<OcrSettingsPage, OcrSettingsViewModel>();
            ViewModelLocationProvider.Register<WorkflowSettingsPage, WorkflowSettingsViewModel>();

            ViewModelLocationProvider.Register<LogisticsCodeRecognitionPage, LogisticsCodeRecognitionViewModel>();
            ViewModelLocationProvider.Register<PackageExitDefinitionPage, PackageExitDefinitionViewModel>();
            ViewModelLocationProvider.Register<SortingInstructionBindingPage, SortingInstructionBindingViewModel>();
            ViewModelLocationProvider.Register<SortingSchemeSettingsPage, SortingSchemeSettingsViewModel>();
            ViewModelLocationProvider.Register<CommunicationsSettingsPage, CommunicationsSettingsViewModel>();
            //接口
            ViewModelLocationProvider.Register<DefaultApiPage, DefaultApiPageViewModel>();
            //其他插件
            {
                ViewModelLocationProvider.Register<SunnenInputBarcodeControl, SunnenInputBarcodeViewModel>();
            }
        }
    }
}