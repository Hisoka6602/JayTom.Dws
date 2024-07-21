using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Views.Editors.CloudService;
using JayTom.Dws.Client.Views.Editors.CameraConfiguration;
using JayTom.Dws.Client.ViewModels.Editors.CameraConfiguration;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration {

    /// <summary>
    /// IPC/NVR管理
    /// </summary>
    public class NvrIpcDeviceManagementViewModel : BindableBase {

        private ObservableCollection<IpcNvrItemInfoModel> _ipcNvrItemInfos = new()
        {
            new IpcNvrItemInfoModel()
            {
                IpAddress = "10.200.211.98",
                Port = 37777,
                Username = "admin",
                Password = "a12345678"
            }
        };

        private bool _isRefreshing;
        private SnackbarMessageQueue _nvrIpcDeviceManagemenMessageQueue = new(TimeSpan.FromSeconds(2));

        public ObservableCollection<IpcNvrItemInfoModel> IpcNvrItemInfos {
            get => _ipcNvrItemInfos;
            set => SetProperty(ref _ipcNvrItemInfos, value);
        }

        public bool IsRefreshing {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public string Identifier => "CameraSettingDialog";

        public SnackbarMessageQueue NvrIpcDeviceManagemenMessageQueue {
            get => _nvrIpcDeviceManagemenMessageQueue;
            set => SetProperty(ref _nvrIpcDeviceManagemenMessageQueue, value);
        }

        public NvrIpcDeviceManagementViewModel() {
        }

        /// <summary>
        /// 预览
        /// </summary>
        public ICommand PreviewCommand => new DelegateCommand<object>(PreviewDelegate);

        private void PreviewDelegate(object obj) {
        }

        /// <summary>
        /// 绑定
        /// </summary>
        public ICommand BindCommand => new DelegateCommand<object>(BindDelegate);

        private void BindDelegate(object obj) {
        }

        /// <summary>
        /// 编辑
        /// </summary>
        public ICommand EditCommand => new DelegateCommand<object>(EditDelegate);

        private void EditDelegate(object obj) {
        }

        /// <summary>
        /// 删除
        /// </summary>
        public ICommand DeleteCommand => new DelegateCommand<object>(DeleteDelegate);

        private void DeleteDelegate(object obj) {
        }

        public ICommand RefreshCommand => new DelegateCommand<object>(RefreshDelegate);

        /// <summary>
        /// 刷新
        /// </summary>
        /// <param name="obj"></param>
        private void RefreshDelegate(object obj) {
        }

        /// <summary>
        /// 添加
        /// </summary>
        public ICommand AddCommand => new DelegateCommand<object>(AddDelegate);

        private async void AddDelegate(object obj) {
            var nvrIpcDeviceEditor = new NvrIpcDeviceEditor();
            if (nvrIpcDeviceEditor.DataContext is NvrIpcDeviceEditorViewModel model) {
                model.Identifier = Identifier;
                /*model.Channel = SelectChannel;
                model.IpAddress = NvrClientSettingsInfo.Ip;
                model.Port = NvrClientSettingsInfo.Port;
                model.Username = NvrClientSettingsInfo.Username;
                model.Password = NvrClientSettingsInfo.Password;*/

                await DialogHost.Show(nvrIpcDeviceEditor, model.Identifier);
                if (!string.IsNullOrEmpty(model.Message)) {
                    NvrIpcDeviceManagemenMessageQueue.Enqueue(model.Message);
                }
            }
        }

        /// <summary>
        /// 批量改密
        /// </summary>
        public ICommand BatchChangePasswordCommand => new DelegateCommand<object>(BatchChangePasswordDelegate);

        private void BatchChangePasswordDelegate(object obj) {
        }
    }
}