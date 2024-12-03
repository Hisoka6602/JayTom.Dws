using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration {

    public class TcpScanSettingsViewModel : BindableBase {

        public TcpScanSettingsViewModel() {
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj) {
        }
    }
}