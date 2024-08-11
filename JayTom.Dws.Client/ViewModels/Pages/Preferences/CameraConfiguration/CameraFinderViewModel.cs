using System;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Windows;
using Newtonsoft.Json;
using TouchSocket.Core;
using Mono.Unix.Native;
using JayTom.Dws.Camera;
using System.Diagnostics;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using Prism.Services.Dialogs;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Client.Models;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using JayTom.Dws.Data.LocalConf;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Data.LocalConf.IpcNvrConfig;
using JayTom.Dws.Client.Views.Dialog.CameraConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Client.Views.Editors.CameraConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.IpcNvrConfig;
using JayTom.Dws.Client.ViewModels.Dialog.CameraConfiguration;
using JayTom.Dws.Client.ViewModels.Editors.CameraConfiguration;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration {

    public class CameraFinderViewModel : BindableBase {
        private readonly IDeviceService _deviceService;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private readonly IPanoramaCameraConfigRepository _panoramaCameraConfigRepository;
        private readonly IVolumeCameraConfigRepository _volumeCameraConfigRepository;
        private readonly IConfigRepository _configRepository;
        private readonly IDialogService _dialogService;
        private readonly IIpcNvrConfigRepository _ipcNvrConfigRepository;
        private bool _isExecuting;
        private static bool _isLoaded;

        private ObservableCollection<CameraFinderItemInfoModel> _cameraFinderItems = new()/* {
            new CameraFinderItemInfoModel() {
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
                Model = "Hik-6565",
            },
            new CameraFinderItemInfoModel() {
                Num = 3,
                Name = "测试相机名称",
                ConnectionType = ConnectionType.Ethernet,
                CameraType = CameraType.IndustrialCamera,
                SerialNumber = "测试序列号3",
                IpAddress = "192.168.0.1",
                Model = "HK-6565",
                IsOcrSupported = true
            },
            new CameraFinderItemInfoModel() {
                Num = 4,
                Name = "测试相机名称",
                ConnectionType = ConnectionType.Ethernet,
                CameraType = CameraType.ThreeDCamera,
                SerialNumber = "测试序列号4",
                IpAddress = "192.168.0.1",
                Model = "HK-6565",
                BoundType = BoundCameraType.BarcodeScannerCamera,
            },
        }*/;

        private SnackbarMessageQueue _cameraFinderMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isRefreshing;
        private CameraSdkSelectorInfoModel _cameraSdkSelectorInfo = new();
        private OcrSettingsDto _ocrSettingsDto = new();

        public CameraFinderViewModel(IDeviceService deviceService,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IPanoramaCameraConfigRepository panoramaCameraConfigRepository,
            IVolumeCameraConfigRepository volumeCameraConfigRepository,
            IConfigRepository configRepository,
            IDialogService dialogService,
            IIpcNvrConfigRepository ipcNvrConfigRepository) {
            _deviceService = deviceService;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _panoramaCameraConfigRepository = panoramaCameraConfigRepository;
            _volumeCameraConfigRepository = volumeCameraConfigRepository;
            _configRepository = configRepository;
            _dialogService = dialogService;
            _ipcNvrConfigRepository = ipcNvrConfigRepository;

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
                        BoundType = s.IsOcrSupported ? CameraBindingType.OcrCamera : CameraBindingType.ScannerCamera,
                        ConnectionType = (CameraConnectionType)s.ConnectionType,
                        CameraType = (CameraType)s.CameraType,
                        HasBinding = true,
                        IpAddress = s.IpAddress,
                        Model = s.Model,
                        Name = s.Name,
                        SerialNumber = s.SerialNumber,
                        Version = s.Version,
                        CustomName = s.CustomName,
                        IsOcrSupported = s.IsOcrSupported,
                    })?.ToList() ?? new List<CameraFinderItemInfoModel>());
                    infoModels.AddRange(panoramaCameraConfigInfoModels.Select(s => new CameraFinderItemInfoModel {
                        BoundType = CameraBindingType.PanoramaCamera,
                        ConnectionType = (CameraConnectionType)s.ConnectionType,
                        CameraType = (CameraType)s.CameraType,
                        HasBinding = true,
                        IpAddress = s.IpAddress,
                        Model = s.Model,
                        Name = s.Name,
                        SerialNumber = s.SerialNumber,
                        CustomName = s.CustomName,
                        Version = s.Version,
                    })?.ToList() ?? new List<CameraFinderItemInfoModel>());
                    infoModels.AddRange(volumeCameraConfigInfoModels.Select(s => new CameraFinderItemInfoModel {
                        BoundType = CameraBindingType.VolumeCamera,
                        ConnectionType = (CameraConnectionType)s.ConnectionType,
                        CameraType = (CameraType)s.CameraType,
                        HasBinding = true,
                        IpAddress = s.IpAddress,
                        Model = s.Model,
                        Name = s.Name,
                        SerialNumber = s.SerialNumber,
                        CustomName = s.CustomName,
                        Version = s.Version,
                    })?.ToList() ?? new List<CameraFinderItemInfoModel>());
                    await Application.Current.Dispatcher.BeginInvoke(async () => {
                        CameraFinderItems.Clear();
                        await Task.Delay(300);
                        list = list.OrderBy(f => f.SerialNumber).ToList();
                        for (var i = 0; i < list.Count; i++) {
                            list[i].Num = i + 1;
                            bool? hasBinding = null;
                            if (!_ocrSettingsDto.IsUseOcr) {
                                //判断是否开启Ocr算法
                                if (infoModels?.FirstOrDefault(f => f.SerialNumber.Equals(list[i].SerialNumber))
                                        ?.BoundType == CameraBindingType.OcrCamera) {
                                    UnbindDelegate(list[i]);
                                    hasBinding = false;
                                }
                                list[i].IsOcrSupported = false;
                            }
                            hasBinding ??= infoModels?.Any(a => a.SerialNumber.Equals(list[i].SerialNumber)) ?? false;
                            list[i].HasBinding = hasBinding ?? false;
                            list[i].BoundType = infoModels?.FirstOrDefault(f => f.SerialNumber.Equals(list[i].SerialNumber))?.BoundType ??
                                                CameraBindingType.ScannerCamera;
                            list[i].CustomName = infoModels?.FirstOrDefault(f => f.SerialNumber.Equals(list[i].SerialNumber))?.CustomName ??
                                                string.Empty;
                        }
                        CameraFinderItems.AddRange(list);
                    });
                }).ConfigureAwait(false).GetAwaiter();
            };
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async settings => {
                if (settings is SettingsChangedEvent { SettingsName: "OcrSettings" }) {
                    var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("OcrSettings"));
                    try {
                        _ocrSettingsDto = JsonConvert.DeserializeObject<OcrSettingsDto>(configInfoModel.Value) ?? new OcrSettingsDto();
                        if (!_ocrSettingsDto.IsUseOcr) {
                            //判断有没有Ocr相机
                            var itemInfoModels = CameraFinderItems.Where(w => w.BoundType == CameraBindingType.OcrCamera)?.ToList();
                            if (itemInfoModels?.Any() == true) {
                                foreach (var cameraFinderItemInfoModel in itemInfoModels) {
                                    //解绑
                                    cameraFinderItemInfoModel.IsOcrSupported = false;
                                    UnbindDelegate(cameraFinderItemInfoModel);
                                }
                            }
                        }
                    }
                    catch (Exception e) {
                        _ocrSettingsDto ??= new OcrSettingsDto();
                    }
                }
            });
        }

        public string Identifier => "CameraSettingDialog";

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

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        /// <summary>
        /// 刷新中
        /// </summary>
        public bool IsRefreshing {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        private async void LoadedDelegate(object obj) {
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
                                IsUseWayzimSmartCameraSdk = cameraSdkSelectorDto.IsUseWayzimSmartCameraSdk,
                                IsUseDaHuaVolumeCameraSdk = cameraSdkSelectorDto.IsUseDaHuaVolumeCameraSdk,
                                IsUseHikvisionVolumeCameraSdk = cameraSdkSelectorDto.IsUseHikvisionVolumeCameraSdk,
                                IsUseDimensionVolumeCameraSdk = cameraSdkSelectorDto.IsUseDimensionVolumeCameraSdk,
                                IsUsbCameraSdk = cameraSdkSelectorDto.IsUsbCameraSdk,
                            };
                        }
                    }
                    configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("OcrSettings"));
                    _ocrSettingsDto = JsonConvert.DeserializeObject<OcrSettingsDto>(configInfoModel?.Value ?? string.Empty) ?? new OcrSettingsDto();
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
        public ICommand RefreshCommand => new DelegateCommand<object>(RefreshDelegate);

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
        public ICommand BindPanoramaCameraCommand => new DelegateCommand<CameraFinderItemInfoModel>(BindPanoramaCameraDelegate);

        private async void BindPanoramaCameraDelegate(CameraFinderItemInfoModel obj) {
            if (_isExecuting || !CheckSdk(obj)) {
                return;
            }
            //判断有没有选中对应的SDK
            var cameraConnectionParameters = string.Empty;
            var failureMessage = string.Empty;
            //判断是否安防相机
            if (obj.CameraType == CameraType.VideoCamera) {
                //弹出账号密码录入框

                //判断是否已登录

                var ipcNvrConfigInfoModels = await _ipcNvrConfigRepository.MemoryCacheData();

                var ipcNvrConfigInfoModel = ipcNvrConfigInfoModels.FirstOrDefault(a => a.SerialNumber.Equals(obj.SerialNumber) &&
                    !a.Username.Equals(string.Empty) &&
                    !a.Password.Equals(string.Empty));
                if (ipcNvrConfigInfoModel is not null) {
                    cameraConnectionParameters = JsonConvert.SerializeObject(new {
                        UserName = ipcNvrConfigInfoModel.Username,
                        PassWord = ipcNvrConfigInfoModel.Password,
                    });
                }
                else {
                    var result = ButtonResult.No;
                    _dialogService.ShowDialog($"VideoCameraSettingsDialog", new DialogParameters()
                    {
                        {"SerialNo", obj.SerialNumber}
                    }, async callback => {
                        result = callback.Result;
                        var userName = callback.Parameters.GetValue<string>("UserName");
                        var passWord = callback.Parameters.GetValue<string>("PassWord");
                        failureMessage = callback.Parameters.GetValue<string>("FailureMessage");

                        cameraConnectionParameters = JsonConvert.SerializeObject(new {
                            UserName = userName,
                            PassWord = passWord,
                        });
                        //更新到库
                        await _ipcNvrConfigRepository.InsertOrUpdate(new IpcNvrConfigInfoModel() {
                            Brand = "DaHua", //当前只有大华
                            IpAddress = obj.IpAddress,
                            Name = obj.Name,
                            Password = passWord,
                            Port = 37777,
                            Type = 0,
                            Username = userName,
                            ChannelCount = 1
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
            }

            //判断指定扫码相机
            var scanCameraSelectionResult = ButtonResult.No;
            var selectedCameraSerialNumber = string.Empty;
            _dialogService.ShowDialog($"ScanCameraSelectionDialog", new DialogParameters()
            {
                {"Cameras",_cameraFinderItems}
            }, callback => {
                scanCameraSelectionResult = callback.Result;
                if (scanCameraSelectionResult == ButtonResult.OK) {
                    var itemInfoModel = callback.Parameters.GetValue<CameraFinderItemInfoModel>("SelectedCamera");
                    selectedCameraSerialNumber = itemInfoModel.SerialNumber;
                }
            });
            if (scanCameraSelectionResult != ButtonResult.OK) {
                return;
            }
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                _isExecuting = true;
                var isSuccess = false;

                var insertOrUpdate = await _panoramaCameraConfigRepository.InsertOrUpdate(new PanoramaCameraConfigInfoModel() {
                    ConnectionType = (int)obj.ConnectionType,
                    CameraType = (int)obj.CameraType,
                    IpAddress = obj.IpAddress,
                    CaptureDelayTime = 100,
                    Model = obj.Model,
                    Name = obj.Name,
                    SerialNumber = obj.SerialNumber,
                    SelectedCameraSerialNumber = selectedCameraSerialNumber,
                    Version = obj.Version,
                    CustomName = obj.CustomName,
                    CameraConnectionParameters = cameraConnectionParameters
                });
                if (insertOrUpdate) {
                    //从数据库修改或增加
                    //触发修改事件
                    obj.HasBinding = true;
                    obj.BoundType = CameraBindingType.PanoramaCamera;
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
        public ICommand BindBarcodeScannerCameraCommand => new DelegateCommand<CameraFinderItemInfoModel>(BindBarcodeScannerCameraDelegate);

        private async void BindBarcodeScannerCameraDelegate(CameraFinderItemInfoModel obj) {
            if (_isExecuting || !CheckSdk(obj)) {
                return;
            }
            var failureMessage = string.Empty;
            var cameraConnectionParameters = string.Empty;
            var result = ButtonResult.No;
            if (obj.CameraType == CameraType.SmartCamera /*&&
                (obj.Brand.Contains("Hik") || obj.Brand.Contains("Dahua") || obj.Model.Contains("DH"))*/) {
                //弹出触发选择
                _dialogService.ShowDialog("TriggerModeSelectionPage", new DialogParameters()
                {
                    {"Brand", obj.Brand}
                }, callback => {
                    //获取参数
                    result = callback.Result;
                    var triggerMode = callback.Parameters.GetValue<TriggerMode>("CameraTriggerMode");
                    var sourceLine = callback.Parameters.GetValue<int>("SourceLine");
                    cameraConnectionParameters = JsonConvert.SerializeObject(new {
                        TriggerMode = triggerMode,
                        SourceLine = sourceLine
                    });
                });
                if (result != ButtonResult.OK) {
                    return;
                }
            }
            //如果是安防相机，需要判断是否已经登录
            //判断是否安防相机
            if (obj.CameraType == CameraType.VideoCamera) {
                //弹出账号密码录入框

                //判断是否已登录

                var ipcNvrConfigInfoModels = await _ipcNvrConfigRepository.MemoryCacheData();

                var ipcNvrConfigInfoModel = ipcNvrConfigInfoModels.FirstOrDefault(a => a.SerialNumber.Equals(obj.SerialNumber) &&
                    !a.Username.Equals(string.Empty) &&
                    !a.Password.Equals(string.Empty));
                if (ipcNvrConfigInfoModel is not null) {
                    cameraConnectionParameters = JsonConvert.SerializeObject(new {
                        UserName = ipcNvrConfigInfoModel.Username,
                        PassWord = ipcNvrConfigInfoModel.Password,
                    });
                }
                else {
                    _dialogService.ShowDialog($"VideoCameraSettingsDialog", new DialogParameters()
                    {
                        {"SerialNo", obj.SerialNumber}
                    }, async callback => {
                        result = callback.Result;
                        var userName = callback.Parameters.GetValue<string>("UserName");
                        var passWord = callback.Parameters.GetValue<string>("PassWord");
                        failureMessage = callback.Parameters.GetValue<string>("FailureMessage");

                        cameraConnectionParameters = JsonConvert.SerializeObject(new {
                            UserName = userName,
                            PassWord = passWord,
                        });
                        //更新到库
                        await _ipcNvrConfigRepository.InsertOrUpdate(new IpcNvrConfigInfoModel() {
                            Brand = "DaHua", //当前只有大华
                            IpAddress = obj.IpAddress,
                            Name = obj.Name,
                            Password = passWord,
                            Port = 37777,
                            Type = 0,
                            Username = userName,
                            ChannelCount = 1
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
                    CustomName = obj.CustomName,
                    CameraConnectionParameters = cameraConnectionParameters
                });
                if (insertOrUpdate) {
                    //从数据库修改或增加
                    //触发修改事件
                    obj.HasBinding = true;
                    obj.BoundType = CameraBindingType.ScannerCamera;
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
        /// 绑定Ocr算法
        /// </summary>
        public ICommand BindOcrScannerCameraCommand => new DelegateCommand<CameraFinderItemInfoModel>(BindOcrScannerCameraDelegate);

        private async void BindOcrScannerCameraDelegate(CameraFinderItemInfoModel obj) {
            //绑定Ocr算法相机
            if (_isExecuting || !CheckSdk(obj)) {
                return;
            }
            //判断是否开启Ocr
            var cameraConnectionParameters = string.Empty;
            var result = ButtonResult.No;
            if (obj.CameraType == CameraType.SmartCamera /*&&
                (obj.Brand.Contains("Hik") || obj.Brand.Contains("Dahua") || obj.Model.Contains("DH"))*/) {
                //弹出触发选择
                _dialogService.ShowDialog("TriggerModeSelectionPage", new DialogParameters()
                {
                    {"Brand", obj.Brand}
                }, callback => {
                    //获取参数
                    result = callback.Result;
                    var triggerMode = callback.Parameters.GetValue<TriggerMode>("CameraTriggerMode");
                    var sourceLine = callback.Parameters.GetValue<int>("SourceLine");
                    cameraConnectionParameters = JsonConvert.SerializeObject(new {
                        TriggerMode = triggerMode,
                        SourceLine = sourceLine
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
                    CustomName = obj.CustomName,
                    CameraConnectionParameters = cameraConnectionParameters,
                    IsOcrSupported = true
                });
                if (insertOrUpdate) {
                    //从数据库修改或增加
                    //触发修改事件
                    obj.HasBinding = true;
                    obj.BoundType = CameraBindingType.OcrCamera;
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
        public ICommand BindVolumeCameraCommand => new DelegateCommand<CameraFinderItemInfoModel>(BindVolumeCameraDelegate);

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
                    CustomName = obj.CustomName,
                    VolumeMeasurementMode = 0,
                    TriggerMode = 0
                });
                if (insertOrUpdate) {
                    //从数据库修改或增加
                    //触发修改事件
                    obj.HasBinding = true;
                    obj.BoundType = CameraBindingType.VolumeCamera;
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
        public ICommand UnbindCommand => new DelegateCommand<CameraFinderItemInfoModel>(UnbindDelegate);

        private async void UnbindDelegate(CameraFinderItemInfoModel obj) {
            if (_isExecuting) {
                return;
            }
            var isSuccess = false;
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                _isExecuting = true;
                if (obj.BoundType is CameraBindingType.ScannerCamera or CameraBindingType.OcrCamera) {
                    //从扫码相机删除
                    var model = await _barcodeScannerCameraConfigRepository.
                        FirstOrDefault(s =>
                            s.SerialNumber.Equals(obj.SerialNumber));
                    if (model is not null) {
                        isSuccess = await _barcodeScannerCameraConfigRepository.Delete(model);
                    }
                }
                else if (obj.BoundType == CameraBindingType.PanoramaCamera) {
                    //从全景相机删除
                    var model = await _panoramaCameraConfigRepository.
                        FirstOrDefault(f => f.SerialNumber.Equals(obj.SerialNumber));
                    if (model is not null) {
                        isSuccess = await _panoramaCameraConfigRepository.Delete(model);
                    }
                }
                else if (obj.BoundType == CameraBindingType.VolumeCamera) {
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

        public ICommand SdkSelectedCommand => new DelegateCommand<object>(SdkSelectedDelegate);

        private void SdkSelectedDelegate(object obj) {
            //检查对应环境
            //判断是否安装了对应SDK的必要程序
            //判断运行目录是否包含必要Sdk
            //如果不满足任意条件则取消选择

            //检查写出文件，后续使用环境变量后删除
            var destinationDir = AppDomain.CurrentDomain.BaseDirectory;
            var files = new List<string>();
            if (obj.ToString()?.Equals("IsUseHikvisionSmartCameraSdk") == true) {
                //海康智能相机
                files = Directory.GetFiles($"{destinationDir}Cameras\\SmartCamera\\Hikvision\\Dll")?.ToList();
            }
            if (obj.ToString()?.Equals("IsUseHikvisionIndustrialCameraSdk") == true) {
                //海康工业相机
                files = Directory.GetFiles($"{destinationDir}Cameras\\IndustrialCamera\\Hikvision\\Dll")?.ToList();
            }
            if (obj.ToString()?.Equals("IsUseHikvisionVolumeCameraSdk") == true) {
                //海康体积
                files = Directory.GetFiles($"{destinationDir}Cameras\\VolumeCamera\\Hikvision\\Dll")?.ToList();
            }
            if (obj.ToString()?.Equals("IsUseDaHuaSmartCameraSdk") == true) {
                //大华智能相机
                files = Directory.GetFiles($"{destinationDir}Cameras\\SmartCamera\\Irayple\\Dll")?.ToList();
            }
            if (obj.ToString()?.Equals("IsUseDaHuaVolumeCameraSdk") == true) {
                //大华体积
                files = Directory.GetFiles($"{destinationDir}Cameras\\VolumeCamera\\Irayple\\Dll")?.ToList();
            }
            if (obj.ToString()?.Equals("IsUseDaHuaSecurityCameraSdk") == true) {
                //大华安防
                files = Directory.GetFiles($"{destinationDir}Cameras\\SecurityCamera\\DaHuatech\\Dll")?.ToList();
            }
            if (obj.ToString()?.Equals("IsUseWayzimSmartCameraSdk") == true) {
                //中科微至
                files = Directory.GetFiles($"{destinationDir}Cameras\\SmartCamera\\Wayzim\\Dll")?.ToList();
            }

            if (obj.ToString()?.Equals("IsUseWayzimIndustrialCameraSdk") == true) {
                //中科工业
                files = Directory.GetFiles($"{destinationDir}Cameras\\IndustrialCamera\\Wayzim\\Dll")?.ToList();
            }
            if (obj.ToString()?.Equals("IsUseDimensionVolumeCameraSdk") == true) {
                //量方体积
                files = Directory.GetFiles($"{destinationDir}Cameras\\VolumeCamera\\Dimension\\Dll")?.ToList();
            }
            if (obj.ToString()?.Equals("IsUsbCameraSdk") == true) {
                //Usb相机
            }
            if (files?.Any() == true) {
                foreach (var s in files.Where(s => !File.Exists($"{destinationDir}\\{new FileInfo(s).Name}"))) {
                    File.Copy(s, $"{destinationDir}\\{new FileInfo(s).Name}", true);
                }
            }
        }

        public ICommand SdkSelectionChangedCommand => new DelegateCommand<object>(SdkSelectionChangedDelegate);

        private async void SdkSelectionChangedDelegate(object obj) {
            //保存到配置
            var cameraSdkSelectorDto = new CameraSdkSelectorDto {
                IsUseDaHuaSecurityCameraSdk = CameraSdkSelectorInfo.IsUseDaHuaSecurityCameraSdk,
                IsUseDaHuaSmartCameraSdk = CameraSdkSelectorInfo.IsUseDaHuaSmartCameraSdk,
                IsUseHikvisionIndustrialCameraSdk = CameraSdkSelectorInfo.IsUseHikvisionIndustrialCameraSdk,
                IsUseHikvisionSmartCameraSdk = CameraSdkSelectorInfo.IsUseHikvisionSmartCameraSdk,
                IsUseWayzimIndustrialCameraSdk = CameraSdkSelectorInfo.IsUseWayzimIndustrialCameraSdk,
                IsUseWayzimSmartCameraSdk = CameraSdkSelectorInfo.IsUseWayzimSmartCameraSdk,
                IsUseDaHuaVolumeCameraSdk = CameraSdkSelectorInfo.IsUseDaHuaVolumeCameraSdk,
                IsUseHikvisionVolumeCameraSdk = CameraSdkSelectorInfo.IsUseHikvisionVolumeCameraSdk,
                IsUseDimensionVolumeCameraSdk = CameraSdkSelectorInfo.IsUseDimensionVolumeCameraSdk,
                IsUsbCameraSdk = CameraSdkSelectorInfo.IsUsbCameraSdk
            };
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = "CameraSdkSelector",
                Value = JsonConvert.SerializeObject(cameraSdkSelectorDto)
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

        public ICommand EditedCustomNameCommand => new DelegateCommand<CameraFinderItemInfoModel>(EditedCustomNameDelegate);

        private async void EditedCustomNameDelegate(CameraFinderItemInfoModel obj) {
            //保存到数据库
            //更新
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                if (obj.HasBinding) {
                    if (obj.BoundType is CameraBindingType.OcrCamera or CameraBindingType.ScannerCamera) {
                        var barcodeScannerCameraConfigInfoModel = await _barcodeScannerCameraConfigRepository.FirstOrDefault(f =>
                            f.SerialNumber.Equals(obj.SerialNumber));
                        if (barcodeScannerCameraConfigInfoModel is not null) {
                            barcodeScannerCameraConfigInfoModel.CustomName = obj.CustomName;
                            var update = await _barcodeScannerCameraConfigRepository.Update(barcodeScannerCameraConfigInfoModel);
                            if (!update) {
                                CameraFinderMessageQueue.Enqueue("修改失败");
                            }
                        }
                    }
                    else if (obj.BoundType is CameraBindingType.PanoramaCamera) {
                        var panoramaCameraConfigInfoModel = await _panoramaCameraConfigRepository.
                            FirstOrDefault(f => f.SerialNumber.Equals(obj.SerialNumber));
                        if (panoramaCameraConfigInfoModel is not null) {
                            panoramaCameraConfigInfoModel.CustomName = obj.CustomName;

                            var update = await _panoramaCameraConfigRepository.Update(panoramaCameraConfigInfoModel);
                            if (!update) {
                                CameraFinderMessageQueue.Enqueue("修改失败");
                            }
                        }
                    }
                    else if (obj.BoundType is CameraBindingType.VolumeCamera) {
                        var volumeCameraConfigInfoModel = await _volumeCameraConfigRepository.
                            FirstOrDefault(f =>
                                f.SerialNumber.Equals(obj.SerialNumber));
                        if (volumeCameraConfigInfoModel is not null) {
                            volumeCameraConfigInfoModel.CustomName = obj.CustomName;
                            var update = await _volumeCameraConfigRepository.Update(volumeCameraConfigInfoModel);
                            if (!update) {
                                CameraFinderMessageQueue.Enqueue("修改失败");
                            }
                        }
                    }
                }
            });
            Keyboard.ClearFocus();
        }

        private bool CheckSdk(CameraFinderItemInfoModel obj) {
            //判断有没有选中对应的SDK
            if ((obj.Brand.Contains("Hikrobot") || obj.Brand.Contains("Hikvision")) &&
                obj.CameraType == CameraType.IndustrialCamera &&
                !CameraSdkSelectorInfo.IsUseHikvisionIndustrialCameraSdk) {
                //海康工业
                CameraFinderMessageQueue.Enqueue("未勾选对应的SDK，无法绑定该相机");
                return false;
            }
            if ((obj.Brand.Contains("Hikrobot") || obj.Brand.Contains("Hikvision")) &&
                obj.CameraType == CameraType.SmartCamera &&
                !CameraSdkSelectorInfo.IsUseHikvisionSmartCameraSdk) {
                //海康智能
                CameraFinderMessageQueue.Enqueue("未勾选对应的SDK，无法绑定该相机");
                return false;
            }
            if ((obj.Brand.Contains("Hikrobot") || obj.Brand.Contains("Hikvision")) &&
                obj.CameraType == CameraType.ThreeDCamera &&
                !CameraSdkSelectorInfo.IsUseHikvisionVolumeCameraSdk) {
                //海康体积
                CameraFinderMessageQueue.Enqueue("未勾选对应的SDK，无法绑定该相机");
                return false;
            }
            if ((obj.Brand.Contains("Dahua") || obj.Brand.Contains("Huaray")) &&
                obj.CameraType == CameraType.SmartCamera &&
                !CameraSdkSelectorInfo.IsUseDaHuaSmartCameraSdk) {
                //大华智能
                CameraFinderMessageQueue.Enqueue("未勾选对应的SDK，无法绑定该相机");
                return false;
            }
            if ((obj.Brand.Contains("Dahua") || obj.Brand.Contains("Huaray")) &&
                obj.CameraType == CameraType.VideoCamera &&
                !CameraSdkSelectorInfo.IsUseDaHuaSecurityCameraSdk) {
                //大华安防
                CameraFinderMessageQueue.Enqueue("未勾选对应的SDK，无法绑定该相机");
                return false;
            }
            if ((obj.Brand.Contains("Dahua") || obj.Brand.Contains("Huaray")) &&
                obj.CameraType == CameraType.ThreeDCamera &&
                !CameraSdkSelectorInfo.IsUseDaHuaVolumeCameraSdk) {
                //大华体积
                CameraFinderMessageQueue.Enqueue("未勾选对应的SDK，无法绑定该相机");
                return false;
            }
            if (obj.Brand.Contains("Wayzim") &&
                obj.CameraType == CameraType.SmartCamera &&
                !CameraSdkSelectorInfo.IsUseWayzimSmartCameraSdk) {
                //中科智能
                CameraFinderMessageQueue.Enqueue("未勾选对应的SDK，无法绑定该相机");
                return false;
            }
            if (obj.Brand.Contains("Wayzim") &&
                obj.CameraType == CameraType.IndustrialCamera &&
                !CameraSdkSelectorInfo.IsUseWayzimIndustrialCameraSdk) {
                //中科工业
                CameraFinderMessageQueue.Enqueue("未勾选对应的SDK，无法绑定该相机");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 绑定Nvr
        /// </summary>
        public ICommand BindNvrCommand => new DelegateCommand<object>(BindNvrDelegate);

        private async void BindNvrDelegate(object obj) {
            //绑定Nvr
            //弹窗

            if (obj is CameraFinderItemInfoModel info) {
                var nvrBindingEditor = new NvrBindingEditor();
                if (nvrBindingEditor.DataContext is NvrBindingEditorViewModel model) {
                    model.Identifier = Identifier;
                    model.NvrBindingParamInfoModel = new NvrBindingParamInfoModel() {
                        BindingSource = SourceType.Camera,
                        DisplayIdentifier = info.SerialNumber,
                        SerialNumber = info.SerialNumber
                    };

                    await DialogHost.Show(nvrBindingEditor, model.Identifier);
                }
            }
        }
    }
}