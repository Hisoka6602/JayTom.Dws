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
using JayTom.Dws.Plugin;
using JayTom.Dws.Camera;
using System.Configuration;
using System.Windows.Media;
using JayTom.Dws.Plugin.Ftp;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Windows.Interop;
using JayTom.Dws.Client.Views;
using JayTom.Dws.Plugin.Excel;
using System.Windows.Threading;
using JayTom.Dws.Plugin.Speech;
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
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.ViewModels.Pages;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.ViewModels.Editors;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Service.ImageStorage;
using JayTom.Dws.Client.Service.ResultOutput;
using Microsoft.Extensions.DependencyInjection;
using JayTom.Dws.Client.Views.Pages.Preferences;
using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Client.ViewModels.Pages.Preferences;
using DryIoc.Microsoft.DependencyInjection.Extension;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.Camera.Cameras.SmartCamera.Hikvision;
using JayTom.Dws.Client.Views.Pages.Preferences.CameraConfiguration;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration;

namespace JayTom.Dws.Client {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication {
        private IHost? _host;

        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            //注册窗口
            containerRegistry.RegisterDialog<ApiAccessDialog>();
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
                //相机
                containerRegistry.RegisterForNavigation<BarcodeScannerCameraConfigPage>();
                containerRegistry.RegisterForNavigation<CameraFinderPage>();
                containerRegistry.RegisterForNavigation<PanoramaCameraConfigPage>();
                containerRegistry.RegisterForNavigation<VolumeCameraConfigPage>();
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
                services.AddScoped<ITcpCommunication, TcpCommunication>();
                services.AddScoped<ITcpCommunicationClient, TcpCommunicationClient>();
                //电脑注册
                services.AddScoped<IComputer, Computer>();
                //电脑信息上报
                services.AddScoped<IComputerInfoReporter, ComputerInfoReporter>();
                //设备注册
                services.AddScoped<ICamera, HikvisionSmartCamera>();

                services.AddScoped<IDeviceService, DefaultDeviceService>();
                services.AddScoped<IImageStorageService, DefaultImageStorageService>();
                services.AddScoped<IResultOutputService, DefaultResultOutputService>();
            });
        }

        protected override Window CreateShell() {
            return Container.Resolve<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e) {
            this.DispatcherUnhandledException += delegate (object sender, DispatcherUnhandledExceptionEventArgs args) {
                //异常触发
                NLog.LogManager.GetCurrentClassLogger().Error($"{JsonConvert.SerializeObject(args.Exception)}");
            };
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args) {
                //异常触发
                NLog.LogManager.GetCurrentClassLogger().Error($"{JsonConvert.SerializeObject(args.ExceptionObject)}");
            };
            base.OnStartup(e);
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

                        /*services.AddSingleton<IDataUploader, WeciMexicoDvApi>();
                        services.AddSingleton<ITcpCommunication, TcpCommunication>();
                        services.AddSingleton(container.Resolve<IBarcodeScannerService>());*/

                        services.AddSingleton(container.Resolve<IComputer>());
                        services.AddSingleton(container.Resolve<IComputerInfoReporter>());
                        services.AddSingleton(container.Resolve<IFtp>());
                        services.AddSingleton(container.Resolve<ISaveImage>());
                        services.AddSingleton(container.Resolve<ISpeech>());
                        services.AddSingleton(container.Resolve<ITcpCommunication>());
                        services.AddSingleton(container.Resolve<ITcpCommunicationClient>());

                        services.AddSingleton(container.Resolve<IDeviceService>());
                        services.AddSingleton(container.Resolve<IDeviceService>());

                        services.AddSingleton(container.Resolve<IDeviceService>());
                        services.AddSingleton(container.Resolve<IImageStorageService>());
                        services.AddSingleton(container.Resolve<IResultOutputService>());
                        services.AddSingleton(container.Resolve<IBarCodeRepository>());
                        services.AddSingleton(container.Resolve<IBarcodeScannerCameraConfigRepository>());
                        services.AddSingleton(container.Resolve<IPanoramaCameraConfigRepository>());
                        services.AddSingleton(container.Resolve<IVolumeCameraConfigRepository>());
                        services.AddSingleton(container.Resolve<ISoundRepository>());
                        services.AddSingleton(container.Resolve<IConfigRepository>());
                        //补注册

                        services.AddHostedService<ComputerInfoBackgroundService>(); // 注册后台服务
                        services.AddHostedService<ScanProcessBackgroundService>(); // 注册后扫码过程服务
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
            base.OnExit(e);
        }

        protected override void ConfigureViewModelLocator() {
            base.ConfigureViewModelLocator();
            //绑定页面
            ViewModelLocationProvider.Register<ExportDialog, ExportDialogViewModel>();
            ViewModelLocationProvider.Register<LoadingDialog, LoadingDialogViewModel>();
            ViewModelLocationProvider.Register<DataTimeEditor, DataTimeEditorViewModel>();
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
        }
    }
}