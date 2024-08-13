using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Drawing;
using JayTom.Dws.Camera;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Domain.Manager;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Data.LocalConf.IpcNvrConfig;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.ViewModels.Editors.Enums;
using JayTom.Dws.Client.Views.Editors.CloudService;
using JayTom.Dws.Client.Views.Dialog.CameraConfiguration;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Client.Views.Editors.CameraConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf.IpcNvrConfig;
using JayTom.Dws.Client.ViewModels.Dialog.CameraConfiguration;
using JayTom.Dws.Client.ViewModels.Editors.CameraConfiguration;
using JayTom.Dws.Infrastructure.Repository.LocalConf.IpcNvrConfig;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration {

    /// <summary>
    /// IPC/NVR管理
    /// </summary>
    public class NvrIpcDeviceManagementViewModel : BindableBase {
        private readonly IConfigRepository _configRepository;
        private readonly IIpcNvrConfigRepository _ipcNvrConfigRepository;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private List<IpcNvrConfigInfoModel>? _ipcNvrConfigInfoModels;
        private List<BarcodeScannerCameraConfigInfoModel>? _scannerCameraConfigInfoModels;

        private ObservableCollection<IpcNvrItemInfoModel> _ipcNvrItemInfos = new();

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

        public NvrIpcDeviceManagementViewModel(IConfigRepository configRepository,
            IIpcNvrConfigRepository ipcNvrConfigRepository,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository) {
            _configRepository = configRepository;
            _ipcNvrConfigRepository = ipcNvrConfigRepository;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj) {
            _ipcNvrConfigInfoModels = await _ipcNvrConfigRepository.MemoryCacheData();
            _scannerCameraConfigInfoModels = await _barcodeScannerCameraConfigRepository.MemoryCacheData();
            RefreshDelegate(obj);
        }

        /// <summary>
        /// 预览
        /// </summary>
        public ICommand PreviewCommand => new DelegateCommand<object>(PreviewDelegate);

        private async void PreviewDelegate(object obj) {
            //显示预览框

            if (obj is IpcNvrItemInfoModel info) {
                var ipcPreviewDialog = new IpcPreviewDialog();
                if (ipcPreviewDialog.DataContext is IpcPreviewViewModel model) {
                    model.Identifier = Identifier;
                    model.IpcNvrItemInfo = info;

                    //测试水印
                    var baseDaHuatech = BaseDaHuatech.CreateInstance();
                    var (key, value) = await baseDaHuatech.LogIn(info.SerialNumber, info.Username, info.Password);
                    if (key) {
                        for (int i = 0; i < 20; i++) {
                            baseDaHuatech.AddSingleRealTimeWatermark(info.SerialNumber, 1, new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(), $"SF153-{i}", new SecurityCameraWatermarkConfig() {
                                ForegroundColor = Color.Blue,
                                BackgroundColor = Color.SeaGreen,
                                Duration = 8000,
                                MaxWatermarks = 8,
                                Position = SecurityCameraWatermarkConfig.WatermarkPosition.TopLeft
                            });
                            await Task.Delay(700);
                        }
                    }

                    await DialogHost.Show(ipcPreviewDialog, model.Identifier);
                }
            }
        }

        /// <summary>
        /// 绑定
        /// </summary>
        public ICommand BindCommand => new DelegateCommand<object>(BindDelegate);

        private async void BindDelegate(object obj) {
            //显示绑定框
            if (obj is IpcNvrItemInfoModel info) {
                var nvrCameraMappingEditor = new NvrCameraMappingEditor();
                if (nvrCameraMappingEditor.DataContext is NvrCameraMappingEditorViewModel model) {
                    model.Identifier = Identifier;
                    model.IpcNvrItemInfo = info;
                    await DialogHost.Show(nvrCameraMappingEditor, model.Identifier);
                }
            }
        }

        /// <summary>
        /// 设置水印
        /// </summary>
        public ICommand SetWatermarkCommand => new DelegateCommand<object>(SetWatermarkDelegate);

        private async void SetWatermarkDelegate(object obj) {
            if (obj is IpcNvrItemInfoModel { Type: DeviceType.NVR } info) {
                var nvrWatermarkConfigEditor = new NvrWatermarkConfigEditor();
                if (nvrWatermarkConfigEditor.DataContext is NvrWatermarkConfigEditorViewModel model) {
                    model.Identifier = Identifier;
                    model.IpcNvrItemInfo = info;
                    await DialogHost.Show(nvrWatermarkConfigEditor, model.Identifier);
                }
            }
        }

        /// <summary>
        /// 编辑
        /// </summary>
        public ICommand EditCommand => new DelegateCommand<object>(EditDelegate);

        private async void EditDelegate(object obj) {
            if (IsRefreshing) return;
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
                        var insertOrUpdate = await _ipcNvrConfigRepository.InsertOrUpdate(new IpcNvrConfigInfoModel() {
                            Brand = info.Brand,
                            Channel = info.Channel,
                            IpAddress = info.IpAddress,
                            Name = info.Name,
                            Password = info.Password,
                            Port = info.Port,
                            Type = (int)info.Type,
                            Username = info.Username,
                            SerialNumber = info.SerialNumber,
                            ChannelCount = info.ChannelCount
                        });
                        if (!insertOrUpdate) {
                            NvrIpcDeviceManagemenMessageQueue.Enqueue("保存失败!");
                        }

                        RefreshDelegate(obj);
                    }
                }
            }
        }

        /// <summary>
        /// 删除
        /// </summary>
        public ICommand DeleteCommand => new DelegateCommand<object>(DeleteDelegate);

        private async void DeleteDelegate(object obj) {
            if (IsRefreshing) return;
            if (obj is IpcNvrItemInfoModel info) {
                var ipcNvrConfigInfoModel = await _ipcNvrConfigRepository.FirstOrDefault(f => f.IpAddress.Equals(info.IpAddress));
                if (ipcNvrConfigInfoModel is not null) {
                    var delete = await _ipcNvrConfigRepository.Delete(ipcNvrConfigInfoModel);
                    if (!delete) {
                        NvrIpcDeviceManagemenMessageQueue.Enqueue("删除失败!");
                    }

                    RefreshDelegate(obj);
                }
            }
        }

        public ICommand RefreshCommand => new DelegateCommand<object>(RefreshDelegate);

        /// <summary>
        /// 刷新
        /// </summary>
        /// <param name="obj"></param>
        private async void RefreshDelegate(object obj) {
            if (IsRefreshing) return;
            IsRefreshing = true;
            await Task.Run(async () => {
                _ipcNvrConfigInfoModels = await _ipcNvrConfigRepository.MemoryCacheData();
                _scannerCameraConfigInfoModels = await _barcodeScannerCameraConfigRepository.MemoryCacheData();
                var sdkSelectorDto = await _configRepository.FirstOrDefaultEntity<CameraSdkSelectorDto>("CameraSdkSelector") ??
                                       new CameraSdkSelectorDto();
                var daHuaSecurityCameras = new List<CameraInfo>();
                if (sdkSelectorDto?.IsUseDaHuaSecurityCameraSdk == true) {
                    //大华安防相机
                    daHuaSecurityCameras = await new DaHuatechSecurityCamera().EnumerateCameras() ?? new List<CameraInfo>();
                }
                else {
                    await Application.Current.Dispatcher.InvokeAsync(() => {
                        NvrIpcDeviceManagemenMessageQueue.Enqueue("未选中任何IPC/NVR的SDK!");
                    });
                }

                var cameraList = daHuaSecurityCameras;
                var itemInfoModels = _ipcNvrConfigInfoModels?.Select(s => new IpcNvrItemInfoModel {
                    IsConfigured = true,
                    Id = s.Id,
                    DeviceName = s.Name,
                    IpAddress = s.IpAddress,
                    Port = s.Port,
                    Type = (DeviceType)s.Type,
                    Username = s.Username,
                    Password = s.Password,
                    Channel = s.Channel,
                    Brand = s.Brand,
                    ChannelCount = s.ChannelCount,
                    SerialNumber = s.SerialNumber,
                    BindingCameraSerialNumbers = new ObservableCollection<BarcodeScannerCameraItemInfoModel>(_scannerCameraConfigInfoModels?.Where(w => (bool)w.NvrCameraBindingInfos?.Any(a => a.IpAddress.Equals(s.IpAddress)))
                        ?.Select(s1 => new BarcodeScannerCameraItemInfoModel {
                            Name = s1.Name,
                            CustomName = s1.CustomName,
                            CameraType = (CameraType)s1.CameraType,
                            SerialNumber = s1.SerialNumber,
                            IpAddress = s1.IpAddress,
                            Model = s1.Model,
                            Version = s1.Version,
                            ConnectionType = (CameraConnectionType)s1.ConnectionType,
                        })?.ToList() ?? new List<BarcodeScannerCameraItemInfoModel>())
                })?.ToList() ?? new List<IpcNvrItemInfoModel>();
                //取出本地添加的项合并(根据Ip合并)
                var nvrItemInfoModels = cameraList.Select((s, i) => new IpcNvrItemInfoModel {
                    ChannelCount = s.CameraNvrInfo?.ChannelCount ?? 0,
                    IsConfigured = _ipcNvrConfigInfoModels?.Any(a => a.IpAddress.Equals(s.IpAddress)) == true,
                    DeviceName = s.Name,
                    Id = s.Id,
                    SerialNumber = s.SerialNumber,
                    IpAddress = s.IpAddress,
                    Port = s.Port,
                    Username = _ipcNvrConfigInfoModels?.FirstOrDefault(f => f.IpAddress.Equals(s.IpAddress))?.Username ?? string.Empty,
                    Password = _ipcNvrConfigInfoModels?.FirstOrDefault(f => f.IpAddress.Equals(s.IpAddress))?.Password ?? string.Empty,
                    Channel = _ipcNvrConfigInfoModels?.FirstOrDefault(f => f.IpAddress.Equals(s.IpAddress))?.Channel ?? s.CameraNvrInfo?.ChannelCount ?? 0,
                    Model = s.Model,
                    Brand = s.Brand,
                    Type = s.Type == CameraType.NvrDevice ? DeviceType.NVR : DeviceType.IPC,
                    BindingCameraSerialNumbers = new ObservableCollection<BarcodeScannerCameraItemInfoModel>(_scannerCameraConfigInfoModels?.Where(w => (bool)w.NvrCameraBindingInfos?.Any(a => a.IpAddress.Equals(s.IpAddress)))
                        ?.Select(s1 => new BarcodeScannerCameraItemInfoModel {
                            Name = s1.Name,
                            CustomName = s1.CustomName,
                            CameraType = (CameraType)s1.CameraType,
                            SerialNumber = s1.SerialNumber,
                            IpAddress = s1.IpAddress,
                            Model = s1.Model,
                            Version = s1.Version,
                            ConnectionType = (CameraConnectionType)s1.ConnectionType,
                        })?.ToList() ?? new List<BarcodeScannerCameraItemInfoModel>())
                })?.Union(itemInfoModels)?.ToList();

                var ipcNvrItemInfoModels = nvrItemInfoModels?.Select((s, i) => {
                    s.Num = i + 1;
                    return s;
                })?.ToList();

                await Application.Current.Dispatcher.InvokeAsync(async () => {
                    IpcNvrItemInfos.Clear();
                    await Task.Delay(200);
                    IpcNvrItemInfos.AddRange(ipcNvrItemInfoModels);
                    IsRefreshing = false;
                    Parallel.ForEach(IpcNvrItemInfos.Where(w =>
                        !w.Username.Equals(string.Empty) &&
                        !w.Password.Equals(string.Empty) &&
                        !w.Brand.Equals(string.Empty)), async device => {
                            //登录
                            if (device.Brand.Equals("DaHua", StringComparison.InvariantCultureIgnoreCase)) {
                                //大华登录
                                await Application.Current.Dispatcher.InvokeAsync(() => {
                                    device.Status = NvrStatus.LoggingIn;
                                    return Task.CompletedTask;
                                });
                                var baseDaHuatech = BaseDaHuatech.CreateInstance();
                                var (key, value) = await baseDaHuatech.LogIn(device.SerialNumber, device.Username, device.Password);
                                await Application.Current.Dispatcher.InvokeAsync(() => {
                                    device.Status = key ? NvrStatus.Online : NvrStatus.LoginFailed;
                                    return Task.CompletedTask;
                                });
                            }
                        });
                });
            });
        }

        /// <summary>
        /// 添加
        /// </summary>
        public ICommand AddCommand => new DelegateCommand<object>(AddDelegate);

        private async void AddDelegate(object obj) {
            if (IsRefreshing) return;
            var nvrIpcDeviceEditor = new NvrIpcDeviceEditor();
            if (nvrIpcDeviceEditor.DataContext is NvrIpcDeviceEditorViewModel model) {
                model.Identifier = Identifier;
                model.ShowType = EditorOperationType.Add;
                await DialogHost.Show(nvrIpcDeviceEditor, model.Identifier);
                if (!string.IsNullOrEmpty(model.Message)) {
                    NvrIpcDeviceManagemenMessageQueue.Enqueue(model.Message);
                }

                if (model.IsOk) {
                    var insertOrUpdate = await _ipcNvrConfigRepository.InsertOrUpdate(new IpcNvrConfigInfoModel() {
                        Brand = "DaHua",//当前只有大华
                        Channel = model.IpcNvrItemInfo.Channel,
                        IpAddress = model.IpcNvrItemInfo.IpAddress,
                        Name = model.IpcNvrItemInfo.Name,
                        Password = model.IpcNvrItemInfo.Password,
                        Port = model.IpcNvrItemInfo.Port,
                        Type = (int)model.IpcNvrItemInfo.Type,
                        Username = model.IpcNvrItemInfo.Username,
                        ChannelCount = 1
                    });

                    if (!insertOrUpdate) {
                        NvrIpcDeviceManagemenMessageQueue.Enqueue("保存失败!");
                    }

                    RefreshDelegate(obj);
                }
            }
        }

        /// <summary>
        /// 批量改密
        /// </summary>
        public ICommand BatchChangePasswordCommand => new DelegateCommand<object>(BatchChangePasswordDelegate);

        private async void BatchChangePasswordDelegate(object obj) {
            if (IsRefreshing) return;
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
                    var nvrConfigInfoModels = await _ipcNvrConfigRepository.MemoryCacheData();

                    var ipcNvrConfigInfoModels = model.SelectDevices.Select(s => new IpcNvrConfigInfoModel() {
                        Brand = nvrConfigInfoModels.FirstOrDefault(f => f.IpAddress.Equals(s.IpAddress))
                            ?.Brand ?? "DaHua", //当前只有大华
                        Id = nvrConfigInfoModels.FirstOrDefault(f => f.IpAddress.Equals(s.IpAddress))
                            ?.Id ?? 0,
                        Channel = nvrConfigInfoModels.FirstOrDefault(f => f.IpAddress.Equals(s.IpAddress))
                            ?.Channel ?? s.Channel,
                        IpAddress = nvrConfigInfoModels.FirstOrDefault(f => f.IpAddress.Equals(s.IpAddress))
                            ?.IpAddress ?? s.IpAddress,
                        Port = nvrConfigInfoModels.FirstOrDefault(f => f.IpAddress.Equals(s.IpAddress))
                            ?.Port ?? s.Port,
                        Name = nvrConfigInfoModels.FirstOrDefault(f => f.IpAddress.Equals(s.IpAddress))
                            ?.Name ?? s.Name,
                        Type = nvrConfigInfoModels.FirstOrDefault(f => f.IpAddress.Equals(s.IpAddress))
                            ?.Type ?? (int)s.Type,
                        Password = model.IpcNvrItemInfo.Password,
                        Username = model.IpcNvrItemInfo.Username,
                        ChannelCount = nvrConfigInfoModels.FirstOrDefault(f => f.IpAddress.Equals(s.IpAddress))
                            ?.ChannelCount ?? s.ChannelCount,
                        SerialNumber = nvrConfigInfoModels.FirstOrDefault(f => f.IpAddress.Equals(s.IpAddress))
                            ?.SerialNumber ?? s.SerialNumber,
                    })?.ToList() ?? new List<IpcNvrConfigInfoModel>();

                    if (ipcNvrConfigInfoModels?.Any() == true) {
                        var updateRange = await _ipcNvrConfigRepository.InsertOrUpdateRange(ipcNvrConfigInfoModels);
                        if (!updateRange) {
                            NvrIpcDeviceManagemenMessageQueue.Enqueue("保存失败!");
                        }

                        RefreshDelegate(obj);
                    }
                }
            }
        }
    }
}