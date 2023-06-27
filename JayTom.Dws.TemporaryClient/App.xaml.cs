using System;
using Prism.Ioc;
using Prism.Mvvm;
using System.Data;
using System.Linq;
using Prism.DryIoc;
using System.Windows;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Collections.Generic;
using JayTom.Dws.TemporaryClient.ViewModels;

namespace JayTom.Dws.TemporaryClient {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication {

        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
        }

        protected override Window CreateShell() {
            return Container.Resolve<MainWindow>();
        }

        protected override void ConfigureViewModelLocator() {
            base.ConfigureViewModelLocator();
            //绑定页面
            ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
            //LoadingDialogViewModel
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
        }
    }
}