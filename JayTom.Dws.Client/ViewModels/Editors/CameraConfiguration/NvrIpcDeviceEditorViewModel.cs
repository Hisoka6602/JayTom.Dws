using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.Repository.LocalConf.IpcNvrConfig;

namespace JayTom.Dws.Client.ViewModels.Editors.CameraConfiguration {

    public class NvrIpcDeviceEditorViewModel : BindableBase {
        private readonly IDeviceService _deviceService;
        private readonly IIpcNvrConfigRepository _ipcNvrConfigRepository;
        private string _identifier = string.Empty;
        private string _message = string.Empty;
        private string _ipAddress = string.Empty;
        private int _port;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _isOk;
        private int _channel;

        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public string Message {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public string IpAddress {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        public int Port {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        public string Username {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool IsOk {
            get => _isOk;
            set => SetProperty(ref _isOk, value);
        }

        public int Channel {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }

        public NvrIpcDeviceEditorViewModel(IDeviceService deviceService,
            IIpcNvrConfigRepository ipcNvrConfigRepository) {
            _deviceService = deviceService;
            _ipcNvrConfigRepository = ipcNvrConfigRepository;
        }

        /// <summary>
        /// 页面加载
        /// </summary>
        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private void LoadedDelegate(object obj) {
        }

        /// <summary>
        /// 保存
        /// </summary>
        public ICommand SaveCommand => new DelegateCommand(SaveDelegate);

        private void SaveDelegate() {
            IsOk = true;
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }

        /// <summary>
        /// 取消
        /// </summary>
        public ICommand CancelCommand => new DelegateCommand(CancelDelegate);

        private void CancelDelegate() {
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }
    }
}