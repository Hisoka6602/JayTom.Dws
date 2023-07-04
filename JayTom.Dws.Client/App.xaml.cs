using System;
using Prism.Ioc;
using Prism.Mvvm;
using System.Data;
using System.Linq;
using Prism.DryIoc;
using System.Windows;
using System.Configuration;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Interop;
using JayTom.Dws.Client.Views;
using System.Collections.Generic;
using JayTom.Dws.Client.ViewModels;
using JayTom.Dws.Client.Views.Pages;
using JayTom.Dws.Client.ViewModels.Pages;
using JayTom.Dws.Client.Views.Pages.Preferences;

namespace JayTom.Dws.Client {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication {

        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            //跳转注册
            {
                containerRegistry.RegisterForNavigation<PluginMarketplacePage>();
                containerRegistry.RegisterForNavigation<DataManagementPage>();
            }
        }

        protected override Window CreateShell() {
            return Container.Resolve<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            // 启用硬件加速
            RenderOptions.ProcessRenderMode = RenderMode.Default;
        }

        protected override void ConfigureViewModelLocator() {
            base.ConfigureViewModelLocator();
            //绑定页面
            ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
            ViewModelLocationProvider.Register<SettingsPage, SettingsPageModel>();
        }
    }
}