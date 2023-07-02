using System;
using DryIoc;
using Prism.Ioc;
using Prism.Mvvm;
using System.Data;
using System.Linq;
using Prism.DryIoc;
using System.Windows;
using System.Net.Http;
using System.Threading;
using JayTom.Dws.Plugin;
using System.Configuration;
using JayTom.Dws.Interface;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using JayTom.Dws.Plugin.Excel;
using System.Windows.Threading;
using JayTom.Dws.Plugin.Speech;
using JayTom.Dws.Infrastructure;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using JayTom.Dws.TemporaryClient.Views;
using JayTom.Dws.Interface.WeciMexicoDv;
using JayTom.Dws.TemporaryClient.Service;
using Microsoft.Extensions.Configuration;
using JayTom.Dws.TemporaryClient.ViewModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.TemporaryClient.Views.Dialog;
using JayTom.Dws.TemporaryClient.Views.Editors;
using Microsoft.Extensions.DependencyInjection;
using JayTom.Dws.TemporaryClient.ViewModels.Dialog;
using JayTom.Dws.TemporaryClient.ViewModels.Editors;
using DryIoc.Microsoft.DependencyInjection.Extension;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.TemporaryClient.Service.BackgroundService;

namespace JayTom.Dws.TemporaryClient {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication {
        private IHost? _host;

        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
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
                services.AddScoped<IConfigRepository, ConfigRepository>();
                services.AddScoped<IBarCodeRepository, BarCodeRepository>();
                //服务注册
                services.AddScoped<IBarcodeScannerService, BarcodeScannerService>();
                //插件注册
                services.AddScoped<ISpeech, Speech>();
                services.AddScoped<IExcel, NpoiExport>();
            });
        }

        protected override Window CreateShell() {
            return Container.Resolve<MainWindow>();
        }

        protected override void ConfigureViewModelLocator() {
            base.ConfigureViewModelLocator();
            //绑定页面
            ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
            ViewModelLocationProvider.Register<DataTimeEditor, DataTimeEditorViewModel>();
            ViewModelLocationProvider.Register<ExportDialog, ExportDialogViewModel>();
            ViewModelLocationProvider.Register<LoadingDialog, LoadingDialogViewModel>();
        }

        protected override void OnStartup(StartupEventArgs e) {
            //启动函数
            this.DispatcherUnhandledException += delegate (object sender, DispatcherUnhandledExceptionEventArgs args) {
                //异常触发
            };
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args) {
                //异常触发
            };
            base.OnStartup(e);
            /*//指定配置文件
            new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();*/

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
                            ServerCertificateCustomValidationCallback = (m, c, ch, e) => true,
                            UseProxy = false
                        };

                        return handler;
                    });

                    services.AddSingleton<IDataUploader, WeciMexicoDvApi>();
                    services.AddSingleton<ITcpCommunication, TcpCommunication>();
                    services.AddSingleton(container.Resolve<IBarcodeScannerService>());
                    services.AddHostedService<BarcodeScannerBackgroundService>(); // 注册后台服务
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
    }
}