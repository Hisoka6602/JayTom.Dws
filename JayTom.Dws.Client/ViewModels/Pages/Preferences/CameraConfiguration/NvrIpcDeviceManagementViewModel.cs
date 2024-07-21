using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.ViewModels.Editors.Enums;
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
                Password = "a12345678",
                Status = NvrStatus.LoggingIn,
                IsConfigured = true,
            },
            new IpcNvrItemInfoModel()
            {
                IpAddress = "10.200.211.98",
                Port = 37777,
                Username = "admin",
                Password = "a12345678",
                Status = NvrStatus.Online,
                IsConfigured = true,
            },
            new IpcNvrItemInfoModel()
            {
                IpAddress = "10.200.211.98",
                Port = 37777,
                Username = "admin",
                Password = "a12345678",
                Status = NvrStatus.Unverified,
            },
            new IpcNvrItemInfoModel()
            {
                IpAddress = "10.200.211.98",
                Port = 37777,
                Username = "admin",
                Password = "a12345678",
                Status = NvrStatus.Offline,
            },
            new IpcNvrItemInfoModel()
            {
                IpAddress = "10.200.211.98",
                Port = 37777,
                Username = "admin",
                Password = "a12345678",
                Status = NvrStatus.LoginFailed,
                IsConfigured = true,
            },
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
            //显示预览框
        }

        /// <summary>
        /// 绑定
        /// </summary>
        public ICommand BindCommand => new DelegateCommand<object>(BindDelegate);

        private void BindDelegate(object obj) {
            //显示绑定框
        }

        /// <summary>
        /// 编辑
        /// </summary>
        public ICommand EditCommand => new DelegateCommand<object>(EditDelegate);

        private async void EditDelegate(object obj) {
            if (obj is IpcNvrItemInfoModel info) {
                var nvrIpcDeviceEditor = new NvrIpcDeviceEditor();
                if (nvrIpcDeviceEditor.DataContext is NvrIpcDeviceEditorViewModel model) {
                    model.Identifier = Identifier;
                    model.ShowType = EditorOperationType.Edit;
                    model.IpcNvrItemInfo = info;

                    await DialogHost.Show(nvrIpcDeviceEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.Message)) {
                        NvrIpcDeviceManagemenMessageQueue.Enqueue(model.Message);
                    }

                    if (model.IsOk) {
                        //更新到库
                        //重新读取数据
                        //自动登录
                    }
                }
            }
        }

        /// <summary>
        /// 删除
        /// </summary>
        public ICommand DeleteCommand => new DelegateCommand<object>(DeleteDelegate);

        private async void DeleteDelegate(object obj) {
            if (obj is IpcNvrItemInfoModel info) {
                //从数据库删除
                //重新读取数据

                //临时展示
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    //临时展示
                    IpcNvrItemInfos.Remove(info);
                    for (var i = 0; i < IpcNvrItemInfos.Count; i++) {
                        IpcNvrItemInfos[i].Num = i + 1;
                    }
                });
            }
        }

        public ICommand RefreshCommand => new DelegateCommand<object>(RefreshDelegate);

        /// <summary>
        /// 刷新
        /// </summary>
        /// <param name="obj"></param>
        private void RefreshDelegate(object obj) {
            //查找设备
            //合并设备(数据库和实时)
            //逐个登录设备
        }

        /// <summary>
        /// 添加
        /// </summary>
        public ICommand AddCommand => new DelegateCommand<object>(AddDelegate);

        private async void AddDelegate(object obj) {
            var nvrIpcDeviceEditor = new NvrIpcDeviceEditor();
            if (nvrIpcDeviceEditor.DataContext is NvrIpcDeviceEditorViewModel model) {
                model.Identifier = Identifier;
                model.ShowType = EditorOperationType.Add;
                await DialogHost.Show(nvrIpcDeviceEditor, model.Identifier);
                if (!string.IsNullOrEmpty(model.Message)) {
                    NvrIpcDeviceManagemenMessageQueue.Enqueue(model.Message);
                }

                if (model.IsOk) {
                    //添加到库
                    //重新读取库的数据展示
                    await Application.Current.Dispatcher.InvokeAsync(() => {
                        //临时展示
                        IpcNvrItemInfos.Add(new IpcNvrItemInfoModel() {
                            Port = model.IpcNvrItemInfo.Port,
                            Username = model.IpcNvrItemInfo.Username,
                            Password = model.IpcNvrItemInfo.Password,
                            Type = model.IpcNvrItemInfo.Type,
                            DeviceName = model.IpcNvrItemInfo.DeviceName,
                            IpAddress = model.IpcNvrItemInfo.IpAddress,
                            IsConfigured = true
                        });
                        for (var i = 0; i < IpcNvrItemInfos.Count; i++) {
                            IpcNvrItemInfos[i].Num = i + 1;
                        }
                    });
                    //自动登录设备
                }
            }
        }

        /// <summary>
        /// 批量改密
        /// </summary>
        public ICommand BatchChangePasswordCommand => new DelegateCommand<object>(BatchChangePasswordDelegate);

        private async void BatchChangePasswordDelegate(object obj) {
            var nvrIpcDeviceEditor = new NvrIpcDeviceEditor();
            if (nvrIpcDeviceEditor.DataContext is NvrIpcDeviceEditorViewModel model) {
                model.Identifier = Identifier;
                model.ShowType = EditorOperationType.BatchChangePassword;
                model.SelectDevices.Clear();
                model.SelectDevices.AddRange(IpcNvrItemInfos.Where(w => w.IsSelect));
                await DialogHost.Show(nvrIpcDeviceEditor, model.Identifier);
                if (!string.IsNullOrEmpty(model.Message)) {
                    NvrIpcDeviceManagemenMessageQueue.Enqueue(model.Message);
                }

                if (model.IsOk) {
                    //登录设备
                    //更新数据库
                }
            }
        }
    }
}