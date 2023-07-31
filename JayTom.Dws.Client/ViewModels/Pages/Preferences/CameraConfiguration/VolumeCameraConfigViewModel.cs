using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using JayTom.Dws.Client.Models;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.PluginInterface.Utils;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Infrastructure.Repository.LocalConf;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration {

    public class VolumeCameraConfigViewModel : BindableBase {
        private readonly IDeviceService _deviceService;
        private readonly IVolumeCameraConfigRepository _volumeCameraConfigRepository;

        private ObservableCollection<VolumeCameraItemInfoModel> _volumeCameraItems = new()
        {
            new VolumeCameraItemInfoModel() {
                Num = 1,
                Name = "测试相机名称",
                ConnectionType = ConnectionType.Ethernet,
                CameraType = CameraType.IndustrialCamera,
                SerialNumber = "1111-2222-3333-4444",
                IpAddress = "192.168.0.1",
                Model = "HK-6565",
                VolumeMeasurementMode=0,
                MinSyncTime=200,
                MaxSyncTime=3000,
                MinLength=1000,
                MaxLength=3000,
                TriggerMode=0,
            },
            new VolumeCameraItemInfoModel() {
                Num = 2,
                Name = "测试相机名称",
                ConnectionType = ConnectionType.Ethernet,
                CameraType = CameraType.IndustrialCamera,
                SerialNumber = "1111-2222-3333-4444",
                IpAddress = "192.168.0.1",
                Model = "HK-6565",
                VolumeMeasurementMode=0,
                MinSyncTime=200,
                MaxSyncTime=3000,
                MinLength=1000,
                MaxLength=3000,
                TriggerMode=0,
            },
        };

        private ObservableCollection<TriggerModeDisplay> _triggerModeDisplayItems = new()
        {
            new TriggerModeDisplay()
            {
                Display = "触发模式1",
                TriggerMode = 0
            },
            new TriggerModeDisplay()
            {
                Display = "触发模式2",
                TriggerMode = 1
            },
        };

        private ObservableCollection<VolumeMeasurementModeDisplay> _volumeMeasurementModeItems = new();

        private SnackbarMessageQueue _volumeCameraMessageQueue = new(TimeSpan.FromSeconds(2));
        private VolumeMeasurementModeDisplay _volumeMeasurementMode = new();
        private TriggerModeDisplay _triggerMode = new();
        private int _minSyncTime;
        private int _maxSyncTime;
        private double _minLength;
        private double _maxLength;

        public VolumeCameraConfigViewModel(IDeviceService deviceService,
            IVolumeCameraConfigRepository volumeCameraConfigRepository) {
            _deviceService = deviceService;
            _volumeCameraConfigRepository = volumeCameraConfigRepository;
            _deviceService.CameraBound += async delegate (object? sender, CameraFinderItemInfoModel model) {
                if (model.BoundType == BoundCameraType.PanoramicCamera) {
                    await Application.Current.Dispatcher.InvokeAsync(async () => {
                        //增加到集合,从数据库获取
                        var infoModel = await _volumeCameraConfigRepository.FirstOrDefault(f =>
                            f.SerialNumber.Equals(model.SerialNumber));
                        if (infoModel is not null) {
                            VolumeCameraItems.Add(new VolumeCameraItemInfoModel() {
                                ConnectionType = (ConnectionType)infoModel.ConnectionType,
                                CameraType = (CameraType)infoModel.CameraType,
                                IpAddress = infoModel.IpAddress,
                                Name = infoModel.Name,
                                SerialNumber = infoModel.SerialNumber,
                                Version = infoModel.Version,
                                Model = infoModel.Model,
                                MinLength = infoModel.MinLength,
                                MaxLength = infoModel.MaxLength,
                                MinSyncTime = infoModel.MinSyncTime,
                                MaxSyncTime = infoModel.MaxSyncTime,
                                TriggerMode = infoModel.TriggerMode,
                                VolumeMeasurementMode = infoModel.VolumeMeasurementMode,
                                Num = VolumeCameraItems.Count + 1,
                            });
                        }
                    });
                }
            };
            _deviceService.CameraUnbound += async delegate (object? sender, CameraFinderItemInfoModel model) {
                //解绑相机,更新列表
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    var infoModel =
                        VolumeCameraItems.FirstOrDefault(f => f.SerialNumber.Equals(model.SerialNumber));
                    if (infoModel is not null) {
                        VolumeCameraItems.Remove(infoModel);
                        //重新排列
                        for (var i = 0; i < VolumeCameraItems.Count; i++) {
                            VolumeCameraItems[i].Num = i + 1;
                        }
                    }
                });
            };
        }

        public SnackbarMessageQueue VolumeCameraMessageQueue {
            get => _volumeCameraMessageQueue;
            set => SetProperty(ref _volumeCameraMessageQueue, value);
        }

        public ObservableCollection<VolumeCameraItemInfoModel> VolumeCameraItems {
            get => _volumeCameraItems;
            set => SetProperty(ref _volumeCameraItems, value);
        }

        public ObservableCollection<TriggerModeDisplay> TriggerModeDisplayItems {
            get => _triggerModeDisplayItems;
            set => SetProperty(ref _triggerModeDisplayItems, value);
        }

        public ObservableCollection<VolumeMeasurementModeDisplay> VolumeMeasurementModeItems {
            get => _volumeMeasurementModeItems;
            set => SetProperty(ref _volumeMeasurementModeItems, value);
        }

        /// <summary>
        /// 体积测量模式
        /// </summary>
        public VolumeMeasurementModeDisplay VolumeMeasurementMode {
            get => _volumeMeasurementMode;
            set => SetProperty(ref _volumeMeasurementMode, value);
        }

        /// <summary>
        /// 最小同步时间（单位：毫秒）
        /// </summary>
        public int MinSyncTime {
            get => _minSyncTime;
            set => SetProperty(ref _minSyncTime, value);
        }

        /// <summary>
        /// 最大同步时间（单位：毫秒）
        /// </summary>
        public int MaxSyncTime {
            get => _maxSyncTime;
            set => SetProperty(ref _maxSyncTime, value);
        }

        /// <summary>
        /// 最小长度
        /// </summary>
        public double MinLength {
            get => _minLength;
            set => SetProperty(ref _minLength, value);
        }

        /// <summary>
        /// 最大长度
        /// </summary>
        public double MaxLength {
            get => _maxLength;
            set => SetProperty(ref _maxLength, value);
        }

        /// <summary>
        /// 触发模式
        /// </summary>
        public TriggerModeDisplay TriggerMode {
            get => _triggerMode;
            set => SetProperty(ref _triggerMode, value);
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private void LoadedDelegate(object obj) {
            LoadCameraItems();
        }

        /// <summary>
        /// 解绑
        /// </summary>
        public ICommand UnbindCameraCommand {
            get => new DelegateCommand<VolumeCameraItemInfoModel>(UnbindCameraDelegate);
        }

        private async void UnbindCameraDelegate(VolumeCameraItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var isSuccess = false;
                var model = await _volumeCameraConfigRepository.
                    FirstOrDefault(s =>
                        s.SerialNumber.Equals(obj.SerialNumber));
                if (model is not null) {
                    var delete = await _volumeCameraConfigRepository.Delete(model);
                    if (delete) {
                        var (key, value) = await _deviceService.OnCameraUnbound(new CameraFinderItemInfoModel() {
                            BoundType = BoundCameraType.BarcodeScannerCamera,
                            ConnectionType = obj.ConnectionType,
                            HasBinding = false,
                            IpAddress = obj.IpAddress,
                            CameraType = obj.CameraType,
                            Model = obj.Model,
                            Name = obj.Name,
                            SerialNumber = obj.SerialNumber,
                            Version = obj.Version,
                            Num = obj.Num,
                        });
                        isSuccess = key;
                    }
                }
                VolumeCameraMessageQueue.Enqueue($"相机:{obj.Name},解绑{(isSuccess ? "成功" : "失败")}");
            });
        }

        /// <summary>
        /// 保存选择项修改
        /// </summary>
        public ICommand ApplyChangesCommand {
            get => new DelegateCommand<VolumeCameraItemInfoModel>(ApplyChangesDelegate);
        }

        private async void ApplyChangesDelegate(VolumeCameraItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var isSuccess = false;
                var infoModel = await _volumeCameraConfigRepository.
                    FirstOrDefault(f =>
                        f.SerialNumber.Equals(obj.SerialNumber));
                if (infoModel is not null) {
                    infoModel.MaxSyncTime = MaxSyncTime;
                    infoModel.MinSyncTime = MinSyncTime;
                    infoModel.TriggerMode = TriggerMode.TriggerMode;
                    infoModel.VolumeMeasurementMode = VolumeMeasurementMode.VolumeMeasurementMode;
                    infoModel.MaxLength = MaxLength;
                    infoModel.MinLength = MinLength;

                    var update = await _volumeCameraConfigRepository.Update(infoModel);
                    if (update) {
                        var (key, value) = await _deviceService.OnCameraParametersModified(new List<CameraParametersModifiedEventArgs>()
                        {
                            new()
                            {
                                Type = BoundCameraType.VolumeCamera,
                                Parameters = infoModel
                            }
                        });
                        isSuccess = key;
                    }
                }
                VolumeCameraMessageQueue.Enqueue($"相机:{obj.Name},保存{(isSuccess ? "成功" : "失败")}");
            });
        }

        /// <summary>
        /// 应用全部更改
        /// </summary>
        public ICommand ApplyAllChangesCommand {
            get => new DelegateCommand<object>(ApplyAllChangesDelegate);
        }

        private async void ApplyAllChangesDelegate(object obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var isSuccess = false;
                var infoModels = await _volumeCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                if (infoModels?.Any() == true) {
                    foreach (var model in infoModels) {
                        model.MaxSyncTime = MaxSyncTime;
                        model.MinSyncTime = MinSyncTime;
                        model.TriggerMode = TriggerMode.TriggerMode;
                        model.VolumeMeasurementMode = VolumeMeasurementMode.VolumeMeasurementMode;
                        model.MaxLength = MaxLength;
                        model.MinLength = MinLength;
                    }

                    var updateRange = await _volumeCameraConfigRepository.UpdateRange(infoModels);
                    if (updateRange) {
                        var list = infoModels?.Select(s => new CameraParametersModifiedEventArgs {
                            Type = BoundCameraType.VolumeCamera,
                            Parameters = infoModels
                        })?.ToList();

                        var (key, value) = await _deviceService.OnCameraParametersModified(list ?? new List<CameraParametersModifiedEventArgs>());
                        isSuccess = key;
                        if (isSuccess) {
                            LoadCameraItems();
                        }
                    }
                }

                //从数据库修改
                //触发修改事件
                VolumeCameraMessageQueue.Enqueue($"保存{(isSuccess ? "成功" : "失败")}");
            });
        }

        private async void LoadCameraItems() {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                VolumeCameraItems.Clear();
                await Task.Delay(100);
                var infoModels = await _volumeCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                var itemInfoModels = infoModels.Select((s, i) => new VolumeCameraItemInfoModel() {
                    ConnectionType = (ConnectionType)s.ConnectionType,
                    CameraType = (CameraType)s.CameraType,
                    IpAddress = s.IpAddress,
                    MaxSyncTime = s.MaxSyncTime,
                    MinSyncTime = MinSyncTime,
                    TriggerMode = s.TriggerMode,
                    VolumeMeasurementMode = s.VolumeMeasurementMode,
                    MaxLength = MaxLength,
                    MinLength = MinLength,
                    Model = s.Model,
                    Name = s.Name,
                    Num = i + 1,
                    SerialNumber = s.SerialNumber,
                    Version = s.Version
                })?.ToList();
                VolumeCameraItems.AddRange(itemInfoModels);
            });
        }
    }

    public class TriggerModeDisplay : BindableBase {
        private int _triggerMode;
        private string _display = string.Empty;

        public int TriggerMode {
            get => _triggerMode;
            set => SetProperty(ref _triggerMode, value);
        }

        public string Display {
            get => _display;
            set => SetProperty(ref _display, value);
        }
    }

    public class VolumeMeasurementModeDisplay : BindableBase {
        private int _volumeMeasurementMode;
        private string _display = string.Empty;

        public int VolumeMeasurementMode {
            get => _volumeMeasurementMode;
            set => SetProperty(ref _volumeMeasurementMode, value);
        }

        public string Display {
            get => _display;
            set => SetProperty(ref _display, value);
        }
    }
}