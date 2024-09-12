using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Models.UpdaterModels;

namespace JayTom.Dws.Client.ViewModels.Dialog.UpdaterDialog {

    public class UpgradePromptViewModel : BindableBase, IDialogAware {
        private VersionUpdateInfoModel _versionUpdateInfo = new();

        public VersionUpdateInfoModel VersionUpdateInfo {
            get => _versionUpdateInfo;
            set => SetProperty(ref _versionUpdateInfo, value);
        }

        public ICommand CloseDialogCommand => new DelegateCommand<object>(CloseDialogDelegate);

        private void CloseDialogDelegate(object obj) {
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }

        public bool CanCloseDialog() {
            return true;
        }

        public void OnDialogClosed() {
        }

        public void OnDialogOpened(IDialogParameters parameters) {
            var info = parameters.GetValue<VersionUpdateInfoModel>("VersionUpdateInfo");
            if (info is not null) {
                VersionUpdateInfo = info;
            }
        }

        public string Title => "版本升级";

        public event Action<IDialogResult>? RequestClose;
    }
}