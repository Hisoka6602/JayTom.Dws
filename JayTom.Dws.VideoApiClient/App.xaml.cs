using Prism.Ioc;
using Prism.Mvvm;
using System.Data;
using Prism.DryIoc;
using System.Windows;
using System.Net.Http;
using System.Configuration;
using JayTom.Dws.VideoApiClient.Api;
using Microsoft.Extensions.Configuration;
using JayTom.Dws.VideoApiClient.ViewModels;
using JayTom.Dws.VideoApiClient.Views.Dialog;
using JayTom.Dws.VideoApiClient.Views.Editors;
using Microsoft.Extensions.DependencyInjection;
using JayTom.Dws.VideoApiClient.ViewModels.Dialog;
using JayTom.Dws.VideoApiClient.ViewModels.Editors;
using DryIoc.Microsoft.DependencyInjection.Extension;

namespace JayTom.Dws.VideoApiClient {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication {
        private IConfiguration _configuration;

        public App() {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            containerRegistry.RegisterDialog<VideoDialog>();
            containerRegistry.GetContainer().RegisterServices(services => {
                services.AddHttpClient("INSURANCE", option => {
                    /*option.Timeout = TimeSpan.FromSeconds(10);*/
                    option.DefaultRequestHeaders.ConnectionClose = true;
                }).ConfigureHttpMessageHandlerBuilder(builder => {
                    builder.PrimaryHandler = new HttpClientHandler {
                        UseDefaultCredentials = true,
                        MaxConnectionsPerServer = 800,
                        ServerCertificateCustomValidationCallback = (m, c, ch, e) => true,
                        UseProxy = false,
                    };

                    //超时重试策略
                });
                services.AddSingleton<IConfiguration>(_configuration);
                services.AddScoped<IVideoApi, VideoApi>();
            });
        }

        protected override Window CreateShell() {
            return Container.Resolve<MainWindow>();
        }

        protected override void ConfigureViewModelLocator() {
            base.ConfigureViewModelLocator();
            //绑定页面
            ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
            ViewModelLocationProvider.Register<LoadingDialog, LoadingDialogViewModel>();
            ViewModelLocationProvider.Register<SettingDialog, SettingDialogViewModel>();
            ViewModelLocationProvider.Register<DataTimeEditor, DataTimeEditorViewModel>();
            ViewModelLocationProvider.Register<VideoDialog, VideoDialogViewModel>();
            //VideoDialog
        }
    }
}