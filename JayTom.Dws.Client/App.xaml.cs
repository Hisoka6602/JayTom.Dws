using System;
using DryIoc;
using Prism.Ioc;
using Prism.Mvvm;
using System.Data;
using System.Linq;
using Prism.DryIoc;
using System.Windows;
using System.Net.Http;
using System.Configuration;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Interop;
using JayTom.Dws.Client.Views;
using System.Windows.Threading;
using JayTom.Dws.Infrastructure;
using JayTom.Dws.Client.Service;
using System.Collections.Generic;
using JayTom.Dws.Client.ViewModels;
using Microsoft.Extensions.Hosting;
using JayTom.Dws.Client.Views.Pages;
using Microsoft.EntityFrameworkCore;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.Views.Editors;
using JayTom.Dws.Client.ViewModels.Pages;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.ViewModels.Editors;
using Microsoft.Extensions.DependencyInjection;
using JayTom.Dws.Client.Views.Pages.Preferences;
using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Client.ViewModels.Pages.Preferences;
using DryIoc.Microsoft.DependencyInjection.Extension;

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
                /*services.AddScoped<IConfigRepository, ConfigRepository>();
                services.AddScoped<IBarCodeRepository, BarCodeRepository>();
                //服务注册
                services.AddScoped<IBarcodeScannerService, BarcodeScannerService>();
                //插件注册
                services.AddScoped<ISpeech, Speech>();
                services.AddScoped<IExcel, NpoiExport>();
                //相机
                services.AddScoped<I3DCamera, Percipio3DCamera>();*/
                //电脑注册
                services.AddScoped<IComputer, Computer>();
                //电脑信息上报
                services.AddScoped<IComputerInfoReporter, ComputerInfoReporter>();
            });
        }

        protected override Window CreateShell() {
            return Container.Resolve<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e) {
            this.DispatcherUnhandledException += delegate (object sender, DispatcherUnhandledExceptionEventArgs args) {
                //异常触发
            };
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args) {
                //异常触发
            };
            base.OnStartup(e);
            // 启用硬件加速
            RenderOptions.ProcessRenderMode = RenderMode.Default;

            // 创建主机并注册后台服务

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
                    //电脑
                    services.AddSingleton(container.Resolve<IComputer>());
                    services.AddSingleton(container.Resolve<IComputerInfoReporter>());
                    services.AddHostedService<ComputerInfoBackgroundService>(); // 注册后台服务
                })
                .Build();
            _host.Start();
        }

        protected override async void OnExit(ExitEventArgs e) {
            base.OnExit(e);
            if (_host is not null) {
                await _host.StopAsync();
                _host.Dispose();
            }
        }

        protected override void ConfigureViewModelLocator() {
            base.ConfigureViewModelLocator();
            //绑定页面
            ViewModelLocationProvider.Register<LoadingDialog, LoadingDialogViewModel>();
            ViewModelLocationProvider.Register<DataTimeEditor, DataTimeEditorViewModel>();
            ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
            ViewModelLocationProvider.Register<SettingsPage, SettingsViewModel>();
            ViewModelLocationProvider.Register<PluginMarketplacePage, PluginMarketplaceViewModel>();
            ViewModelLocationProvider.Register<HomePage, HomeViewModel>();
            ViewModelLocationProvider.Register<StatusBarPage, StatusBarViewModel>();
            ViewModelLocationProvider.Register<ApiAccessDialog, ApiAccessViewModel>();
            ViewModelLocationProvider.Register<DataManagementPage, DataManagementViewModel>();
            //DataManagementViewModel
        }
    }
}