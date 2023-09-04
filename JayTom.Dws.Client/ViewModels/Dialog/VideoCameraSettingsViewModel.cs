using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;

namespace JayTom.Dws.Client.ViewModels.Dialog {
    public class VideoCameraSettingsViewModel : BindableBase, IDialogAware {
        private string _userName = string.Empty;
        private string _passWord = string.Empty;

        public string UserName {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string PassWord {
            get => _passWord;
            set => SetProperty(ref _passWord, value);
        }

        public ICommand CloseCommand {
            get => new DelegateCommand<CameraItemInfoModel>(CloseDelegate);
        }

        private void CloseDelegate(CameraItemInfoModel obj) {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));

        }

        public ICommand OkCommand {
            get => new DelegateCommand<CameraItemInfoModel>(OkDelegate);
        }

        private void OkDelegate(CameraItemInfoModel obj) {

            OnRequestClose(new DialogResult(ButtonResult.OK, new DialogParameters()
            {
                {"UserName",UserName},
                {"PassWord",PassWord},
            }));
        }

        public bool CanCloseDialog() {
            return true;
        }

        public void OnDialogClosed() {
        }

        public void OnDialogOpened(IDialogParameters parameters) {
        }

        public string Title { get; }

        public event Action<IDialogResult>? RequestClose;

        protected virtual void OnRequestClose(IDialogResult obj) {
            RequestClose?.Invoke(obj);
        }
    }
}