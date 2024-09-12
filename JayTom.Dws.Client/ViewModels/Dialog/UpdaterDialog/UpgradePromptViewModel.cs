using System;
using Velopack;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Diagnostics;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Models.UpdaterModels;

namespace JayTom.Dws.Client.ViewModels.Dialog.UpdaterDialog {

    public class UpgradePromptViewModel : BindableBase, IDialogAware {
        private VersionUpdateInfoModel _versionUpdateInfo = new();
        private UpgradeStatus _upgradeStatus = UpgradeStatus.WaitingForUpgrade;
        private UpdateManager? _um;
        private UpdateInfo? _update;
        private int _downloadProgress;
        private string _exceptionMessage = string.Empty;

        public VersionUpdateInfoModel VersionUpdateInfo {
            get => _versionUpdateInfo;
            set => SetProperty(ref _versionUpdateInfo, value);
        }

        public UpgradeStatus UpgradeStatus {
            get => _upgradeStatus;
            set => SetProperty(ref _upgradeStatus, value);
        }

        public int DownloadProgress {
            get => _downloadProgress;
            set => SetProperty(ref _downloadProgress, value);
        }

        public string ExceptionMessage {
            get => _exceptionMessage;
            set => SetProperty(ref _exceptionMessage, value);
        }

        public ICommand ImmediateUpdateCommand => new DelegateCommand<object>(ImmediateUpdateDelegate);

        private async void ImmediateUpdateDelegate(object obj) {
            //立即更新
            if (UpgradeStatus == UpgradeStatus.WaitingForUpgrade &&
                _um is not null && _update is not null) {
                UpgradeStatus = UpgradeStatus.Upgrading;
                try {
                    await _um.DownloadUpdatesAsync(_update, async p => {
                        await Application.Current.Dispatcher.InvokeAsync(() => {
                            DownloadProgress = p;
                            if (DownloadProgress >= 100 && _um.IsUpdatePendingRestart) {
                                _um.ApplyUpdatesAndRestart(_update);
                            }
                        });
                    }).ConfigureAwait(true);
                }
                catch (Exception e) {
                    ExceptionMessage = e.Message;
                    UpgradeStatus = UpgradeStatus.UpgradeFailed;
                }
            }
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
                if (string.IsNullOrEmpty(VersionUpdateInfo.UpdateMessage)) {
                    VersionUpdateInfo.UpdateMessage = "无信息";
                }
            }
            _um = parameters.GetValue<UpdateManager>("UpdateManager");
            _update = parameters.GetValue<UpdateInfo>("UpdateInfo");
        }

        public string Title => "版本升级";

        public event Action<IDialogResult>? RequestClose;
    }
}