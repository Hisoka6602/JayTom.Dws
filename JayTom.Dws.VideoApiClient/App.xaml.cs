using Prism.Ioc;
using System.Data;
using Prism.DryIoc;
using System.Windows;
using System.Configuration;

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
    }
}