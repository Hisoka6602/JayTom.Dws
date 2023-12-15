using Prism.Ioc;
using Prism.Mvvm;
using System.Data;
using Prism.DryIoc;
using System.Windows;
using System.Configuration;
using JayTom.Dws.VideoApiClient.ViewModels;
using JayTom.Dws.VideoApiClient.Views.Dialog;
using JayTom.Dws.VideoApiClient.Views.Editors;
using JayTom.Dws.VideoApiClient.ViewModels.Dialog;
using JayTom.Dws.VideoApiClient.ViewModels.Editors;

namespace JayTom.Dws.VideoApiClient {

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
            ViewModelLocationProvider.Register<LoadingDialog, LoadingDialogViewModel>();
            ViewModelLocationProvider.Register<DataTimeEditor, DataTimeEditorViewModel>();
            //LoadingDialog
        }
    }
}