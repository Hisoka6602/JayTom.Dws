using System;
using Prism.Ioc;
using Prism.Mvvm;
using System.Data;
using System.Linq;
using Prism.DryIoc;
using System.Windows;
using System.Configuration;
using System.Threading.Tasks;
using JayTom.Dws.Client.Views;
using System.Collections.Generic;
using JayTom.Dws.Client.ViewModels;

namespace JayTom.Dws.Client {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication {

        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            //throw new NotImplementedException();
        }

        protected override Window CreateShell() {
            return Container.Resolve<MainWindow>();
        }

        protected override void ConfigureViewModelLocator() {
            base.ConfigureViewModelLocator();
            //绑定页面
            ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
        }
    }
}