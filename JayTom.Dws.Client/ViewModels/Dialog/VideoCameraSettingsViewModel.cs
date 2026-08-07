using System;
using Prism.Mvvm;
using Prism.Commands;
using System.Windows.Input;
using Prism.Services.Dialogs;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;

namespace JayTom.Dws.Client.ViewModels.Dialog
{

    public class VideoCameraSettingsViewModel : BindableBase, IDialogAware
    {
        private string _userName = string.Empty;
        private string _passWord = string.Empty;
        private string _serialNo = string.Empty;

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string PassWord
        {
            get => _passWord;
            set => SetProperty(ref _passWord, value);
        }

        public string SerialNo
        {
            get => _serialNo;
            set => SetProperty(ref _serialNo, value);
        }

        public ICommand CloseCommand => new DelegateCommand<object>(CloseDelegate);

        private void CloseDelegate(object obj)
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        public ICommand OkCommand => new DelegateCommand<object>(OkDelegate);

        private async void OkDelegate(object obj)
        {
            //
            var failureMessage = string.Empty;
            var (key, value) = await BaseDaHuatech.CreateInstance().LogIn(SerialNo, UserName, PassWord);
            if (!key)
            {
                failureMessage = value;
            }
            OnRequestClose(new DialogResult(ButtonResult.OK, new DialogParameters()
            {
                {"UserName",UserName},
                {"PassWord",PassWord},
                {"FailureMessage",failureMessage},
            }));
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            SerialNo = parameters.GetValue<string>("SerialNo");
        }

        public string Title { get; }

        public event Action<IDialogResult>? RequestClose;

        protected virtual void OnRequestClose(IDialogResult obj)
        {
            RequestClose?.Invoke(obj);
        }
    }
}