using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using Newtonsoft.Json;
using Mono.Unix.Native;
using JayTom.Dws.Camera;
using System.Diagnostics;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using JayTom.Dws.Client.Models;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using CameraType = JayTom.Dws.Client.Models.CameraType;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration {
    public class CameraFinderViewModel : BindableBase {
        private readonly IDeviceService _deviceService;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private readonly IPanoramaCameraConfigRepository _panoramaCameraConfigRepository;
        private readonly IVolumeCameraConfigRepository _volumeCameraConfigRepository;
        private readonly IConfigRepository _configRepository;
        private readonly IDialogService _dialogService;
        private bool _isExecuting;
        private static bool _isLoaded;

        private ObservableCollection<CameraFinderItemInfoModel> _cameraFinderItems = new() {
            /*new CameraFinderItemInfoModel() {
                Num = 1,
                Name = "增加一个转换、如果是工业相机、智能相机则不显示体积绑定",
                ConnectionType = ConnectionType.Ethernet,
                CameraType = CameraType.VideoCamera,
                SerialNumber = "测试序列号1",
                IpAddress = "192.168.888.888",
                Model = "在WPF中我需要新建一个绑定相机类型显示的的转换器，请给我代码",
            },
            new CameraFinderItemInfoModel() {
                Num = 2,
                Name = "测试相机名称",
                ConnectionType = ConnectionType.Ethernet,
                CameraType = CameraType.SmartCamera,
                SerialNumber = "测试序列号2",
                IpAddress = "192.168.0.1",
                Model = "HK-6565",
            },
            new CameraFinderItemInfoModel() {
                Num = 3,
                Name = "测试相机名称",
                ConnectionType = ConnectionType.Ethernet,
                CameraType = CameraType.IndustrialCamera,
                SerialNumber = "测试序列号3",
                IpAddress = "192.168.0.1",
                Model = "HK-6565",
                BoundType = BoundCameraType.BarcodeScannerCamera
            },
            new CameraFinderItemInfoModel() {
                Num = 4,
                Name = "测试相机名称",
                ConnectionType = ConnectionType.Ethernet,
                CameraType = CameraType.ThreeDCamera,
                SerialNumber = "测试序列号4",
                IpAddress = "192.168.0.1",
                Model = "HK-6565",
                BoundType = BoundCameraType.BarcodeScannerCamera
            },*/
        };

        private SnackbarMessageQueue _cameraFinderMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isRefreshing;
        private CameraSdkSelectorInfoModel _cameraSdkSelectorInfo = new();

        public CameraFinderViewModel(IDeviceService deviceService,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IPanoramaCameraConfigRepository panoramaCameraConfigRepository,
            IVolumeCameraConfigRepository volumeCameraConfigRepository,
            IConfigRepository configRepository,
            IDialogService dialogService) {
            _deviceService = deviceService;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _panoramaCameraConfigRepository = panoramaCameraConfigRepository;
            _volumeCameraConfigRepository = volumeCameraConfigRepository;
            _configRepository = configRepository;
            _dialogService = dialogService;
            _deviceService.CameraUnbound += async delegate (object? sender, CameraFinderItemInfoModel model) {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    var infoModel = CameraFinderItems.FirstOrDefault(f => f.SerialNumber.Equals(model.SerialNumber));
                    if (infoModel is not null) {
                        infoModel.HasBinding = false;
                    }
                });
            };
            _deviceService.CameraEnumerationRefreshed += async delegate (object? sender, List<CameraFinderItemInfoModel> list) {
                await Task.Delay(100);
                Task.Run(async () => {
                    var infoModels = new List<CameraFinderItemInfoModel>();
                    var scannerCameraConfigInfoModels = await _barcodeScannerCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                    var panoramaCameraConfigInfoModels = await _panoramaCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                    var volumeCameraConfigInfoModels = await _volumeCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                    infoModels.AddRange(scannerCameraConfigInfoModels.Select(s => new CameraFinderItemInfoModel {
                        BoundType = BoundCameraType.BarcodeScannerCamera,
                        ConnectionType = (ConnectionType)s.ConnectionType,
                        CameraType = (CameraType)s.CameraType,
                        HasBinding = true,
                        IpAddress = s.IpAddress,
                        Model = s.Model,
                        Name = s.Name,
                        SerialNumber = s.SerialNumber,
                        Version = s.Version,
                    })?.ToList() ?? new List<CameraFinderItemInfoModel>());
                    infoModels.AddRange(panoramaCameraConfigInfoModels.Select(s => new CameraFinderItemInfoModel {
                        BoundType = BoundCameraType.PanoramicCamera,
                        ConnectionType = (ConnectionType)s.ConnectionType,
                        CameraType = (CameraType)s.CameraType,
                        HasBinding = true,
                        IpAddress = s.IpAddress,
                        Model = s.Model,
                        Name = s.Name,
                        SerialNumber = s.SerialNumber,
                        Version = s.Version,
                    })?.ToList() ?? new List<CameraFinderItemInfoModel>());
                    infoModels.AddRange(volumeCameraConfigInfoModels.Select(s => new CameraFinderItemInfoModel {
                        BoundType = BoundCameraType.VolumeCamera,
                        ConnectionType = (ConnectionType)s.ConnectionType,
                        CameraType = (CameraType)s.CameraType,
                        HasBinding = true,
                        IpAddress = s.IpAddress,
                        Model = s.Model,
                        Name = s.Name,
                        SerialNumber = s.SerialNumber,
                        Version = s.Version,
                    })?.ToList() ?? new List<CameraFinderItemInfoModel>());
                    await Application.Current.Dispatcher.BeginInvoke(async () => {
                        CameraFinderItems.Clear();
                        await Task.Delay(300);
                        list = list.OrderBy(f => f.SerialNumber).ToList();
                        for (var i = 0; i < list.Count; i++) {
                            list[i].Num = i + 1;
                            list[i].HasBinding = infoModels?.Any(a => a.SerialNumber.Equals(list[i].SerialNumber)) ?? false;
                            list[i].BoundType = infoModels?.FirstOrDefault(f => f.SerialNumber.Equals(list[i].SerialNumber))?.BoundType ??
                                                BoundCameraType.BarcodeScannerCamera;
                        }
                        CameraFinderItems.AddRange(list);
                    });
                }).ConfigureAwait(false).GetAwaiter();
            };
        }

        /// <summary>
        /// Sdk选择
        /// </summary>
        public CameraSdkSelectorInfoModel CameraSdkSelectorInfo {
            get => _cameraSdkSelectorInfo;
            set => SetProperty(ref _cameraSdkSelectorInfo, value);
        }

        public SnackbarMessageQueue CameraFinderMessageQueue {
            get => _cameraFinderMessageQueue;
            set => SetProperty(ref _cameraFinderMessageQueue, value);
        }

        public ObservableCollection<CameraFinderItemInfoModel> CameraFinderItems {
            get => _cameraFinderItems;
            set => SetProperty(ref _cameraFinderItems, value);
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadeDelegate);
        }

        /// <summary>
        /// 刷新中
        /// </summary>
        public bool IsRefreshing {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        private async void LoadeDelegate(object obj) {
            //加载相机对比绑定状态
            if (!_isLoaded) {
                _isLoaded = true;
                //读配置
                try {
                    var configInfoModel = await _configRepository.FirstOrDefault(f =>
                        f.ConfigName.Equals("CameraSdkSelector"));
                    if (configInfoModel is not null) {
                        var cameraSdkSelectorDto = JsonConvert.DeserializeObject<CameraSdkSelectorDto>(configInfoModel.Value);
                        if (cameraSdkSelectorDto is not null) {
                            CameraSdkSelectorInfo = new CameraSdkSelectorInfoModel() {
                                IsUseDaHuaSecurityCameraSdk = cameraSdkSelectorDto.IsUseDaHuaSecurityCameraSdk,
                                IsUseDaHuaSmartCameraSdk = cameraSdkSelectorDto.IsUseDaHuaSmartCameraSdk,
                                IsUseHikvisionIndustrialCameraSdk =
                                    cameraSdkSelectorDto.IsUseHikvisionIndustrialCameraSdk,
                                IsUseHikvisionSmartCameraSdk = cameraSdkSelectorDto.IsUseHikvisionSmartCameraSdk,
                                IsUseWayzimIndustrialCameraSdk = cameraSdkSelectorDto.IsUseWayzimIndustrialCameraSdk,
                                IsUseWayzimSmartCameraSdk = cameraSdkSelectorDto.IsUseWayzimSmartCameraSdk
                            };
                        }
                    }
                }
                catch (Exception e) {
                    CameraFinderMessageQueue.Enqueue($"{e.Message}");
                }
                RefreshDelegate(obj);
            }
        }

        /// <summary>
        /// 刷新
        /// </summary>
        public ICommand RefreshCommand {
            get => new DelegateCommand<object>(RefreshDelegate);
        }

        private void RefreshDelegate(object obj) {
            if (IsRefreshing) {
                return;
            }
            IsRefreshing = true;
            Task.Run(async () => {
                var (key, value) = await _deviceService.OnCameraEnumerationRefreshed().ConfigureAwait(false);
                await Application.Current.Dispatcher.BeginInvoke(() => {
                    CameraFinderMessageQueue.Enqueue(key ? $"{Languages.Language.ResourceManager.GetString("已重新枚举相机")}" : value);
                    IsRefreshing = false;
                    return Task.CompletedTask;
                }, DispatcherPriority.Background);
            }).ConfigureAwait(false).GetAwaiter();
        }

        /// <summary>
        ///  绑定全景相机
        /// </summary>
        public ICommand BindPanoramaCameraCommand {
            get => new DelegateCommand<CameraFinderItemInfoModel>(BindPanoramaCameraDelegate);
        }

        private async void BindPanoramaCameraDelegate(CameraFinderItemInfoModel obj) {
            if (_isExecuting) {
                return;
            }
            //判断有没有选中对应的SDK
            var cameraConnectionParameters = string.Empty;
            var failureMessage = string.Empty;
            if (obj.CameraType == CameraType.VideoCamera) {
                //弹出账号密码录入框
                var result = ButtonResult.No;
                _dialogService.ShowDialog($"VideoCameraSettingsDialog", new DialogParameters()
                {
                    {"SerialNo", obj.SerialNumber}
                }, callback => {
                    result = callback.Result;
                    var userName = callback.Parameters.GetValue<string>("UserName");
                    var passWord = callback.Parameters.GetValue<string>("PassWord");
                    failureMessage = callback.Parameters.GetValue<string>("FailureMessage");

                    cameraConnectionParameters = JsonConvert.SerializeObject(new {
                        UserName = userName,
                        PassWord = passWord,
                    });
                });
                if (result != ButtonResult.OK) {
                    return;
                }
                if (!failureMessage.Equals(string.Empty)) {
                    CameraFinderMessageQueue.Enqueue(failureMessage);
                    return;
                }
            }

            await Application.Current.Dispatcher.InvokeAsync(async () => {
                _isExecuting = true;
                var isSuccess = false;
                //判断是否安防相机

                var insertOrUpdate = await _panoramaCameraConfigRepository.InsertOrUpdate(new PanoramaCameraConfigInfoModel() {
                    ConnectionType = (int)obj.ConnectionType,
                    CameraType = (int)obj.CameraType,
                    IpAddress = obj.IpAddress,
                    CaptureDelayTime = 2000,
                    Model = obj.Model,
                    Name = obj.Name,
                    SerialNumber = obj.SerialNumber,
                    Version = obj.Version,
                    CameraConnectionParameters = cameraConnectionParameters
                });
                if (insertOrUpdate) {
                    //从数据库修改或增加
                    //触发修改事件
                    obj.HasBinding = true;
                    obj.BoundType = BoundCameraType.PanoramicCamera;
                    var (key, value) = await _deviceService.OnCameraBound(obj);
                    if (!key) {
                        obj.HasBinding = false;
                    }
                    isSuccess = key;
                }

                CameraFinderMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Camera")}:{obj.Name},{Languages.Language.ResourceManager.GetString("Bind")}{(isSuccess ?
                    Languages.Language.ResourceManager.GetString("Success") :
                    Languages.Language.ResourceManager.GetString("Failure"))}");
                _isExecuting = false;
            });
        }

        /// <summary>
        /// 绑定扫码相机
        /// </summary>
        public ICommand BindBarcodeScannerCameraCommand {
            get => new DelegateCommand<CameraFinderItemInfoModel>(BindBarcodeScannerCameraDelegate);
        }

        private async void BindBarcodeScannerCameraDelegate(CameraFinderItemInfoModel obj) {
            if (_isExecuting) {
                return;
            }
            //判断有没有选中对应的SDK
            if ((obj.Brand.Contains("Hikrobot") || obj.Brand.Contains("Hikvision")) &&
                obj.CameraType == CameraType.IndustrialCamera &&
                !CameraSdkSelectorInfo.IsUseHikvisionIndustrialCameraSdk) {
                //海康工业
                CameraFinderMessageQueue.Enqueue("未勾选对应的SDK，无法绑定该相机");
                return;
            }
            if ((obj.Brand.Contains("Hikrobot") || obj.Brand.Contains("Hikvision")) &&
                obj.CameraType == CameraType.IndustrialCamera &&
                !CameraSdkSelectorInfo.IsUseHikvisionSmartCameraSdk) {
                //海康智能
                CameraFinderMessageQueue.Enqueue("未勾选对应的SDK，无法绑定该相机");
                return;
            }
            if ((obj.Brand.Contains("Dahua") || obj.Brand.Contains("Huaray")) &&
                obj.CameraType == CameraType.SmartCamera &&
                !CameraSdkSelectorInfo.IsUseDaHuaSmartCameraSdk) {
                //大华智能
                CameraFinderMessageQueue.Enqueue("未勾选对应的SDK，无法绑定该相机");
                return;
            }
            if ((obj.Brand.Contains("Dahua") || obj.Brand.Contains("Huaray")) &&
                obj.CameraType == CameraType.VideoCamera &&
                !CameraSdkSelectorInfo.IsUseDaHuaSecurityCameraSdk) {
                //大华安防
                CameraFinderMessageQueue.Enqueue("未勾选对应的SDK，无法绑定该相机");
                return;
            }
            if (obj.Brand.Contains("Wayzim") &&
                obj.CameraType == CameraType.SmartCamera &&
                !CameraSdkSelectorInfo.IsUseWayzimSmartCameraSdk) {
                //中科智能
                CameraFinderMessageQueue.Enqueue("未勾选对应的SDK，无法绑定该相机");
                return;
            }
            //CameraSdkSelectorInfo
            var cameraConnectionParameters = string.Empty;
            var result = ButtonResult.No;
            if (obj.CameraType == CameraType.SmartCamera &&
                obj.Brand.Contains("Hik")) {
                //弹出触发选择
                _dialogService.ShowDialog("TriggerModeSelectionPage", callback => {
                    //获取参数
                    result = callback.Result;
                    var triggerMode = callback.Parameters.GetValue<TriggerMode>("CameraTriggerMode");
                    cameraConnectionParameters = JsonConvert.SerializeObject(new {
                        TriggerMode = triggerMode,
                    });
                });
                if (result != ButtonResult.OK) {
                    return;
                }
            }

            await Application.Current.Dispatcher.InvokeAsync(async () => {
                _isExecuting = true;
                var isSuccess = false;
                var insertOrUpdate = await _barcodeScannerCameraConfigRepository.InsertOrUpdate(new BarcodeScannerCameraConfigInfoModel() {
                    ConnectionType = (int)obj.ConnectionType,
                    CameraType = (int)obj.CameraType,
                    IpAddress = obj.IpAddress,
                    Model = obj.Model,
                    Name = obj.Name,
                    SerialNumber = obj.SerialNumber,
                    Version = obj.Version,
                    IsShowRealTimeImage = true,
                    CameraConnectionParameters = cameraConnectionParameters
                });
                if (insertOrUpdate) {
                    //从数据库修改或增加
                    //触发修改事件
                    obj.HasBinding = true;
                    obj.BoundType = BoundCameraType.BarcodeScannerCamera;
                    var (key, value) = await _deviceService.OnCameraBound(obj);
                    if (!key) {
                        obj.HasBinding = false;
                    }
                    isSuccess = key;
                }

                CameraFinderMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Camera")}:{obj.Name}, {Languages.Language.ResourceManager.GetString("Bind")}{(isSuccess ?
                    Languages.Language.ResourceManager.GetString("Success") : Languages.Language.ResourceManager.GetString("Failure"))}");
                _isExecuting = false;
            });
        }

        /// <summary>
        /// 绑定体积相机
        /// </summary>
        public ICommand BindVolumeCameraCommand {
            get => new DelegateCommand<CameraFinderItemInfoModel>(BindVolumeCameraDelegate);
        }

        private async void BindVolumeCameraDelegate(CameraFinderItemInfoModel obj) {
            if (_isExecuting) {
                return;
            }
            //判断有没有选中对应的SDK
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                _isExecuting = true;
                var isSuccess = false;
                var insertOrUpdate = await _volumeCameraConfigRepository.InsertOrUpdate(new VolumeCameraConfigInfoModel() {
                    ConnectionType = (int)obj.ConnectionType,
                    CameraType = (int)obj.CameraType,
                    IpAddress = obj.IpAddress,
                    Model = obj.Model,
                    Name = obj.Name,
                    SerialNumber = obj.SerialNumber,
                    Version = obj.Version,
                    MaxLength = 2000,
                    MinLength = 1000,
                    MaxSyncTime = 1000,
                    MinSyncTime = 500,
                    VolumeMeasurementMode = 0,
                    TriggerMode = 0
                });
                if (insertOrUpdate) {
                    //从数据库修改或增加
                    //触发修改事件
                    obj.HasBinding = true;
                    obj.BoundType = BoundCameraType.VolumeCamera;
                    var (key, value) = await _deviceService.OnCameraBound(obj);
                    if (!key) {
                        obj.HasBinding = false;
                    }
                    isSuccess = key;
                }

                CameraFinderMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Camera")}:{obj.Name},{Languages.Language.ResourceManager.GetString("Bind")}{(isSuccess ?
                    Languages.Language.ResourceManager.GetString("Success") : Languages.Language.ResourceManager.GetString("Failure"))}");
                _isExecuting = false;
            });
        }

        /// <summary>
        /// 解绑
        /// </summary>
        public ICommand UnbindCommand {
            get => new DelegateCommand<CameraFinderItemInfoModel>(UnbindDelegate);
        }

        private async void UnbindDelegate(CameraFinderItemInfoModel obj) {
            if (_isExecuting) {
                return;
            }
            var isSuccess = false;
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                _isExecuting = true;
                if (obj.BoundType == BoundCameraType.BarcodeScannerCamera) {
                    //从扫码相机删除
                    var model = await _barcodeScannerCameraConfigRepository.
                        FirstOrDefault(s =>
                            s.SerialNumber.Equals(obj.SerialNumber));
                    if (model is not null) {
                        isSuccess = await _barcodeScannerCameraConfigRepository.Delete(model);
                    }
                }
                else if (obj.BoundType == BoundCameraType.PanoramicCamera) {
                    //从全景相机删除
                    var model = await _panoramaCameraConfigRepository.
                        FirstOrDefault(f => f.SerialNumber.Equals(obj.SerialNumber));
                    if (model is not null) {
                        isSuccess = await _panoramaCameraConfigRepository.Delete(model);
                    }
                }
                else if (obj.BoundType == BoundCameraType.VolumeCamera) {
                    //从体积相机删除
                    var model = await _volumeCameraConfigRepository.
                        FirstOrDefault(f => f.SerialNumber.Equals(obj.SerialNumber));
                    if (model is not null) {
                        isSuccess = await _volumeCameraConfigRepository.Delete(model);
                    }
                }
                if (isSuccess) {
                    var (key, value) = await _deviceService.OnCameraUnbound(obj);
                    isSuccess = key;
                }

                CameraFinderMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Camera")}:{obj.Name},{Languages.Language.ResourceManager.GetString("Unbind")}{(isSuccess ?
                    Languages.Language.ResourceManager.GetString("Success") : Languages.Language.ResourceManager.GetString("Failure"))}");
                _isExecuting = false;
            });
        }

        public ICommand SdkSelectedCommand {
            get => new DelegateCommand<object>(SdkSelectedDelegate);
        }

        private void SdkSelectedDelegate(object obj) {
            //检查对应环境
            //判断是否安装了对应SDK的必要程序
            //判断运行目录是否包含必要Sdk
            //如果不满足任意条件则取消选择

            Debug.WriteLine(obj);
        }

        public ICommand SdkSelectionChangedCommand {
            get => new DelegateCommand<object>(SdkSelectionChangedDelegate);
        }

        private async void SdkSelectionChangedDelegate(object obj) {
            //保存到配置
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = "CameraSdkSelector",
                Value = JsonConvert.SerializeObject(new CameraSdkSelectorDto {
                    IsUseDaHuaSecurityCameraSdk = CameraSdkSelectorInfo.IsUseDaHuaSecurityCameraSdk,
                    IsUseDaHuaSmartCameraSdk = CameraSdkSelectorInfo.IsUseDaHuaSmartCameraSdk,
                    IsUseHikvisionIndustrialCameraSdk = CameraSdkSelectorInfo.IsUseHikvisionIndustrialCameraSdk,
                    IsUseHikvisionSmartCameraSdk = CameraSdkSelectorInfo.IsUseHikvisionSmartCameraSdk,
                    IsUseWayzimIndustrialCameraSdk = CameraSdkSelectorInfo.IsUseWayzimIndustrialCameraSdk,
                    IsUseWayzimSmartCameraSdk = CameraSdkSelectorInfo.IsUseWayzimSmartCameraSdk
                })
            });
            if (insertOrUpdate) {
                EventAggregator.Instance.Publish(new SettingsChangedEvent {
                    SettingsName = "CameraSdkSelector"
                });
            }
            else {
                CameraFinderMessageQueue.Enqueue(Languages.Language.ResourceManager.GetString("SaveFailed") ?? string.Empty);
            }
        }
    }
}