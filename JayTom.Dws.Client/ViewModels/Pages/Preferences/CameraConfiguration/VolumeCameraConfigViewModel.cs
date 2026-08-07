using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Windows;
using JayTom.Dws.Camera;
using System.Windows.Input;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration
{

    public class VolumeCameraConfigViewModel : BindableBase
    {
        private readonly IDeviceService _deviceService;
        private readonly IVolumeCameraConfigRepository _volumeCameraConfigRepository;

        private ObservableCollection<VolumeCameraItemInfoModel> _volumeCameraItems = new()
        {
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
        /// <summary>
        /// 相机绑定事件处理器，保存实例以便页面卸载时退订。
        /// </summary>
        private readonly EventHandler<CameraFinderItemInfoModel> _cameraBoundHandler;
        /// <summary>
        /// 相机解绑事件处理器，保存实例以便页面卸载时退订。
        /// </summary>
        private readonly EventHandler<CameraFinderItemInfoModel> _cameraUnboundHandler;
        /// <summary>
        /// 页面事件是否已订阅。
        /// </summary>
        private bool _eventsSubscribed;

        public VolumeCameraConfigViewModel(IDeviceService deviceService,
            IVolumeCameraConfigRepository volumeCameraConfigRepository)
        {
            _deviceService = deviceService;
            _volumeCameraConfigRepository = volumeCameraConfigRepository;
            _cameraBoundHandler = async delegate (object? sender, CameraFinderItemInfoModel model)
            {
                if (model.BoundType == CameraBindingType.VolumeCamera)
                {
                    try
                    {
                        //增加到集合,从数据库获取
                        var infoModel = await _volumeCameraConfigRepository.FirstOrDefault(f =>
                            f.SerialNumber.Equals(model.SerialNumber));
                        if (infoModel is not null)
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                                VolumeCameraItems.Add(new VolumeCameraItemInfoModel
                                {
                                    ConnectionType = (CameraConnectionType)infoModel.ConnectionType,
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
                                    Num = VolumeCameraItems.Count + 1
                                }));
                        }
                    }
                    catch (Exception exception)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                            VolumeCameraMessageQueue.Enqueue(
                                $"加载新绑定体积相机失败:{exception.Message}"));
                    }
                }
            };
            _cameraUnboundHandler = async delegate (object? sender, CameraFinderItemInfoModel model)
            {
                //解绑相机,更新列表
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var infoModel =
                        VolumeCameraItems.FirstOrDefault(f => f.SerialNumber.Equals(model.SerialNumber));
                    if (infoModel is not null)
                    {
                        VolumeCameraItems.Remove(infoModel);
                        //重新排列
                        for (var i = 0; i < VolumeCameraItems.Count; i++)
                        {
                            VolumeCameraItems[i].Num = i + 1;
                        }
                    }
                });
            };
        }

        public SnackbarMessageQueue VolumeCameraMessageQueue
        {
            get => _volumeCameraMessageQueue;
            set => SetProperty(ref _volumeCameraMessageQueue, value);
        }

        public ObservableCollection<VolumeCameraItemInfoModel> VolumeCameraItems
        {
            get => _volumeCameraItems;
            set => SetProperty(ref _volumeCameraItems, value);
        }

        public ObservableCollection<TriggerModeDisplay> TriggerModeDisplayItems
        {
            get => _triggerModeDisplayItems;
            set => SetProperty(ref _triggerModeDisplayItems, value);
        }

        public ObservableCollection<VolumeMeasurementModeDisplay> VolumeMeasurementModeItems
        {
            get => _volumeMeasurementModeItems;
            set => SetProperty(ref _volumeMeasurementModeItems, value);
        }

        /// <summary>
        /// 体积测量模式
        /// </summary>
        public VolumeMeasurementModeDisplay VolumeMeasurementMode
        {
            get => _volumeMeasurementMode;
            set => SetProperty(ref _volumeMeasurementMode, value);
        }

        /// <summary>
        /// 最小同步时间（单位：毫秒）
        /// </summary>
        public int MinSyncTime
        {
            get => _minSyncTime;
            set => SetProperty(ref _minSyncTime, value);
        }

        /// <summary>
        /// 最大同步时间（单位：毫秒）
        /// </summary>
        public int MaxSyncTime
        {
            get => _maxSyncTime;
            set => SetProperty(ref _maxSyncTime, value);
        }

        /// <summary>
        /// 最小长度
        /// </summary>
        public double MinLength
        {
            get => _minLength;
            set => SetProperty(ref _minLength, value);
        }

        /// <summary>
        /// 最大长度
        /// </summary>
        public double MaxLength
        {
            get => _maxLength;
            set => SetProperty(ref _maxLength, value);
        }

        /// <summary>
        /// 触发模式
        /// </summary>
        public TriggerModeDisplay TriggerMode
        {
            get => _triggerMode;
            set => SetProperty(ref _triggerMode, value);
        }

        public ICommand LoadedCommand
        {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        /// <summary>
        /// 页面卸载命令。
        /// </summary>
        public ICommand UnloadedCommand => new DelegateCommand<object>(UnloadedDelegate);

        private void LoadedDelegate(object obj)
        {
            if (!_eventsSubscribed)
            {
                _deviceService.CameraBound += _cameraBoundHandler;
                _deviceService.CameraUnbound += _cameraUnboundHandler;
                _eventsSubscribed = true;
            }

            _ = LoadCameraItemsAsync();
        }

        /// <summary>
        /// 页面卸载时解除设备事件订阅。
        /// </summary>
        private void UnloadedDelegate(object obj)
        {
            if (!_eventsSubscribed)
            {
                return;
            }

            _deviceService.CameraBound -= _cameraBoundHandler;
            _deviceService.CameraUnbound -= _cameraUnboundHandler;
            _eventsSubscribed = false;
        }

        /// <summary>
        /// 解绑
        /// </summary>
        public ICommand UnbindCameraCommand
        {
            get => new DelegateCommand<VolumeCameraItemInfoModel>(UnbindCameraDelegate);
        }

        private async void UnbindCameraDelegate(VolumeCameraItemInfoModel obj)
        {
            try
            {
                var isSuccess = false;
                var model = await _volumeCameraConfigRepository.
                    FirstOrDefault(s =>
                        s.SerialNumber.Equals(obj.SerialNumber));
                if (model is not null)
                {
                    var delete = await _volumeCameraConfigRepository.Delete(model);
                    if (delete)
                    {
                        var (key, value) = await _deviceService.OnCameraUnbound(new CameraFinderItemInfoModel()
                        {
                            BoundType = CameraBindingType.VolumeCamera,
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
                VolumeCameraMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Camera")} : {obj.Name} , {Languages.Language.ResourceManager.GetString("Unbind")}{(isSuccess
                    ? Languages.Language.ResourceManager.GetString("Success") : Languages.Language.ResourceManager.GetString("Failure"))}");
            }
            catch (Exception exception)
            {
                VolumeCameraMessageQueue.Enqueue($"解绑体积相机失败:{exception.Message}");
            }
        }

        /// <summary>
        /// 保存选择项修改
        /// </summary>
        public ICommand ApplyChangesCommand
        {
            get => new DelegateCommand<VolumeCameraItemInfoModel>(ApplyChangesDelegate);
        }

        private async void ApplyChangesDelegate(VolumeCameraItemInfoModel obj)
        {
            try
            {
                var isSuccess = false;
                var infoModel = await _volumeCameraConfigRepository.
                    FirstOrDefault(f =>
                        f.SerialNumber.Equals(obj.SerialNumber));
                if (infoModel is not null)
                {
                    infoModel.MaxSyncTime = MaxSyncTime;
                    infoModel.MinSyncTime = MinSyncTime;
                    infoModel.TriggerMode = TriggerMode.TriggerMode;
                    infoModel.VolumeMeasurementMode = VolumeMeasurementMode.VolumeMeasurementMode;
                    infoModel.MaxLength = MaxLength;
                    infoModel.MinLength = MinLength;

                    var update = await _volumeCameraConfigRepository.Update(infoModel);
                    if (update)
                    {
                        var (key, value) = await _deviceService.OnCameraParametersModified(new List<CameraParametersModifiedEventArgs>()
                        {
                            new()
                            {
                                Type = CameraBindingType.VolumeCamera,
                                Parameters = infoModel
                            }
                        });
                        isSuccess = key;
                    }
                }
                VolumeCameraMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Camera")}:{obj.Name}, {Languages.Language.ResourceManager.GetString("Save") ?? string.Empty}{(isSuccess ?
                    Languages.Language.ResourceManager.GetString("Success") : Languages.Language.ResourceManager.GetString("Failure"))}");
            }
            catch (Exception exception)
            {
                VolumeCameraMessageQueue.Enqueue($"保存体积相机失败:{exception.Message}");
            }
        }

        /// <summary>
        /// 应用全部更改
        /// </summary>
        public ICommand ApplyAllChangesCommand
        {
            get => new DelegateCommand<object>(ApplyAllChangesDelegate);
        }

        private async void ApplyAllChangesDelegate(object obj)
        {
            try
            {
                var isSuccess = false;
                var infoModels = await _volumeCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                if (infoModels?.Any() == true)
                {
                    foreach (var model in infoModels)
                    {
                        model.MaxSyncTime = MaxSyncTime;
                        model.MinSyncTime = MinSyncTime;
                        model.TriggerMode = TriggerMode.TriggerMode;
                        model.VolumeMeasurementMode = VolumeMeasurementMode.VolumeMeasurementMode;
                        model.MaxLength = MaxLength;
                        model.MinLength = MinLength;
                    }

                    var updateRange = await _volumeCameraConfigRepository.UpdateRange(infoModels);
                    if (updateRange)
                    {
                        var list = infoModels?.Select(s => new CameraParametersModifiedEventArgs
                        {
                            Type = CameraBindingType.VolumeCamera,
                            Parameters = s
                        })?.ToList();

                        var (key, value) = await _deviceService.OnCameraParametersModified(list ?? new List<CameraParametersModifiedEventArgs>());
                        isSuccess = key;
                        if (isSuccess)
                        {
                            await LoadCameraItemsAsync();
                        }
                    }
                }

                //从数据库修改
                //触发修改事件
                VolumeCameraMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Save") ?? string.Empty}{(isSuccess ?
                    Languages.Language.ResourceManager.GetString("Success") : Languages.Language.ResourceManager.GetString("Failure"))}");
            }
            catch (Exception exception)
            {
                VolumeCameraMessageQueue.Enqueue($"批量保存体积相机失败:{exception.Message}");
            }
        }

        /// <summary>
        /// 加载体积相机配置列表。
        /// </summary>
        private async Task LoadCameraItemsAsync()
        {
            try
            {
                var infoModels = await _volumeCameraConfigRepository.Select(
                    static camera => camera.Id > 0,
                    static camera => camera.Id);
                var itemInfoModels = infoModels.Select((s, i) => new VolumeCameraItemInfoModel
                {
                    ConnectionType = (CameraConnectionType)s.ConnectionType,
                    CameraType = (CameraType)s.CameraType,
                    IpAddress = s.IpAddress,
                    MaxSyncTime = s.MaxSyncTime,
                    MinSyncTime = s.MinSyncTime,
                    TriggerMode = s.TriggerMode,
                    VolumeMeasurementMode = s.VolumeMeasurementMode,
                    MaxLength = s.MaxLength,
                    MinLength = s.MinLength,
                    Model = s.Model,
                    Name = s.Name,
                    Num = i + 1,
                    SerialNumber = s.SerialNumber,
                    Version = s.Version
                }).ToList();
                VolumeCameraItems.Clear();
                VolumeCameraItems.AddRange(itemInfoModels);
            }
            catch (Exception exception)
            {
                VolumeCameraMessageQueue.Enqueue($"加载体积相机失败:{exception.Message}");
            }
        }
    }

    public class TriggerModeDisplay : BindableBase
    {
        private int _triggerMode;
        private string _display = string.Empty;

        public int TriggerMode
        {
            get => _triggerMode;
            set => SetProperty(ref _triggerMode, value);
        }

        public string Display
        {
            get => _display;
            set => SetProperty(ref _display, value);
        }
    }

    public class VolumeMeasurementModeDisplay : BindableBase
    {
        private int _volumeMeasurementMode;
        private string _display = string.Empty;

        public int VolumeMeasurementMode
        {
            get => _volumeMeasurementMode;
            set => SetProperty(ref _volumeMeasurementMode, value);
        }

        public string Display
        {
            get => _display;
            set => SetProperty(ref _display, value);
        }
    }
}
