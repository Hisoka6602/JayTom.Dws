using System;
using System.IO;
using Dynamsoft;
using System.Linq;
using JayTom.Dws.Ocr;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Camera;
using Newtonsoft.Json.Linq;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Plugin.Scale;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using System.Collections.Concurrent;
using JayTom.Dws.Camera.BarCodeReader;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Plugin.Scale.StaticScale;
using JayTom.Dws.Plugin.Scale.DynamicScale;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf;
using CameraType = JayTom.Dws.Camera.CameraType;
using JayTom.Dws.Domain.Dto.CameraConfiguration;
using JayTom.Dws.Camera.Cameras.SmartCamera.Wayzim;
using JayTom.Dws.Plugin.Scale.ScaleValueParameters;
using JayTom.Dws.Camera.Cameras.SmartCamera.Irayple;
using JayTom.Dws.Camera.Cameras.VolumeCamera.Irayple;
using JayTom.Dws.Camera.Cameras.SmartCamera.Hikvision;
using JayTom.Dws.Camera.Cameras.VolumeCamera.Hikvision;
using JayTom.Dws.Camera.Cameras.VolumeCamera.Dimension;
using JayTom.Dws.Camera.Cameras.IndustrialCamera.Wayzim;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Camera.Cameras.IndustrialCamera.Hikvision;
using JayTom.Dws.Camera.Cameras.IndustrialCamera.UsbCamera;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;

namespace JayTom.Dws.Client.Service.Device {

    public class DefaultDeviceService : IDeviceService {
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private readonly IPanoramaCameraConfigRepository _panoramaCameraConfigRepository;
        private readonly IVolumeCameraConfigRepository _volumeCameraConfigRepository;
        private readonly IConfigRepository _configRepository;
        private readonly IDynamicScale _dynamicScale;
        private readonly IStaticScale _staticScale;
        private readonly IOcr _ocr;
        private readonly IUsbCameraConfigRepository _usbCameraConfigRepository;
        private SemaphoreSlim _cameraSlim = new(1);

        //private List<string> CameraInitializationException { get; set; } = new();
        //private List<CameraInfo> _cameraInfos = new();

        private List<ICamera> _cameras = new();
        private readonly List<CameraParametersModifiedEventArgs> _cameraParameters = new();
        private BarcodeFilterSettingsDto? _barcodeFilterSettingsDto = new();
        private WeightSettingsDto? _weightSettingsDto = new();
        private CameraSdkSelectorDto? _cameraSdkSelectorDto;
        private static ConcurrentDictionary<string, CameraInfo> _cameraInfos = new();
        public bool RunningStatus { get; private set; } = false;
        public List<CameraInfo> CameraItems { get; private set; } = new();
        public ScaleType ScaleType { get; private set; } = ScaleType.None;

        public event EventHandler<List<ICamera>>? CameraInitialized;

        public event EventHandler<List<ICamera>>? CameraDisconnected;

        public event EventHandler<List<ICamera>>? CameraFault;

        public event EventHandler<BarcodeReadEventArgs>? BarcodeScanned;

        public event EventHandler<BarcodeReadEventArgs>? NotBarcodeHitEvent;

        public event EventHandler<PanoramaCaptureEventArgs>? PanoramaCaptured;

        public event EventHandler<VolumeCapturedEventArgs>? VolumeCaptured;

        public event EventHandler<RealTimeImageEventArgs>? RealTimeImage;

        public event EventHandler<List<CameraFinderItemInfoModel>>? CameraEnumerationRefreshed;

        public event EventHandler<DeviceExceptionEventArgs>? CameraException;

        public async Task<KeyValuePair<bool, string>> OnCameraEnumerationRefreshed(CancellationToken token = default) {
            await Task.Yield();
            _cameraInfos.Clear();
            try {
                var configInfoModel = await _configRepository.FirstOrDefault(f =>
                    f.ConfigName.Equals("CameraSdkSelector"), token);
                _cameraSdkSelectorDto = configInfoModel is not null ? JsonConvert.DeserializeObject<CameraSdkSelectorDto>(configInfoModel.Value) : new CameraSdkSelectorDto();

                var daHuaSmartCameras = new List<CameraInfo>();
                var hikvisionIndustrialCameras = new List<CameraInfo>();
                var hikvisionSmartCameras = new List<CameraInfo>();
                var daHuaSecurityCameras = new List<CameraInfo>();
                var wayzimSmartCameras = new List<CameraInfo>();
                var wayzimIndustrialCameras = new List<CameraInfo>();
                var hikvisionVolumeCameras = new List<CameraInfo>();
                var daHuaVolumeCameras = new List<CameraInfo>();
                var dimensionVolumeCamera = new List<CameraInfo>();
                var normalUsbCamera = new List<CameraInfo>();
                //HikvisionVolumeCamera
                //判断已经选择的相机

                if (_cameraSdkSelectorDto?.IsUseDaHuaSmartCameraSdk == true) {
                    //大华智能相机
                    daHuaSmartCameras = await new DaHuaSmartCamera().EnumerateCameras();
                }

                if (_cameraSdkSelectorDto?.IsUseHikvisionIndustrialCameraSdk == true) {
                    //海康工业相机
                    hikvisionIndustrialCameras = await new HikvisionIndustrialCamera().EnumerateCameras();
                }

                if (_cameraSdkSelectorDto?.IsUseHikvisionSmartCameraSdk == true) {
                    //海康智能相机
                    hikvisionSmartCameras = await new HikvisionSmartCamera().EnumerateCameras();
                }
                if (_cameraSdkSelectorDto?.IsUseDaHuaSecurityCameraSdk == true) {
                    //大华安防相机
                    daHuaSecurityCameras = await new DaHuatechSecurityCamera().EnumerateCameras();
                }

                if (_cameraSdkSelectorDto?.IsUseWayzimSmartCameraSdk == true) {
                    //中科微至智能相机
                    wayzimSmartCameras = await new WayzimSmartCamera().EnumerateCameras();
                }
                if (_cameraSdkSelectorDto?.IsUseWayzimIndustrialCameraSdk == true) {
                    //中科微至工业相机
                    wayzimIndustrialCameras = await new WayzimIndustrialCamera().EnumerateCameras();
                }

                if (_cameraSdkSelectorDto?.IsUseHikvisionVolumeCameraSdk == true) {
                    //海康体积相机
                    hikvisionVolumeCameras = await new HikvisionVolumeCamera().EnumerateCameras();
                }

                if (_cameraSdkSelectorDto?.IsUseDaHuaVolumeCameraSdk == true) {
                    //大华体积相机
                    daHuaVolumeCameras = await new DaHuaSmartCamera().EnumerateCameras();
                }

                if (_cameraSdkSelectorDto?.IsUseDimensionVolumeCameraSdk == true) {
                    dimensionVolumeCamera = await new DimensionVolumeCamera().EnumerateCameras();
                }

                if (_cameraSdkSelectorDto?.IsUsbCameraSdk == true) {
                    //Usb相机
                    normalUsbCamera = await new NormalUsbCamera().EnumerateCameras();
                }

                var cameraList = daHuaVolumeCameras?.Union(daHuaSmartCameras
                                                           ?? new List<CameraInfo>())?.ToList()?
                                     .Union(wayzimIndustrialCameras ?? new List<CameraInfo>())?.ToList()?
                                     .Union(wayzimSmartCameras ?? new List<CameraInfo>())?.ToList()?
                                     .Union(daHuaSecurityCameras ?? new List<CameraInfo>())?.ToList()?
                                     .Union(hikvisionIndustrialCameras ?? new List<CameraInfo>())?.ToList()?
                                     .Union(hikvisionSmartCameras ?? new List<CameraInfo>())?.ToList()?
                                     .Union(hikvisionVolumeCameras ?? new List<CameraInfo>())?.ToList()?
                                     .Union(dimensionVolumeCamera ?? new List<CameraInfo>())?.ToList()?
                                     .Union(normalUsbCamera ?? new List<CameraInfo>())?.ToList()
                                 ?? new List<CameraInfo>();
                var list = cameraList.Select(s =>
                    _cameraInfos.AddOrUpdate(s.SerialNumber, s,
                        (k, v) => s))?.ToList();
                var itemInfoModels = list?.Select(s => new CameraFinderItemInfoModel {
                    SerialNumber = s.SerialNumber,
                    Model = s.Model,
                    Name = s.Name,
                    IpAddress = s.IpAddress,
                    ConnectionType = s.ConnectionType,
                    CameraType = ConvertCameraType(s.Brand, s.Model),
                    Version = s.Version,
                    Brand = s.Brand,
                    IsOcrSupported = s.IsOcrSupported
                })?.ToList();
                CameraEnumerationRefreshed?.Invoke(null, itemInfoModels ?? new List<CameraFinderItemInfoModel>());
                CameraItems = list ?? new List<CameraInfo>();
                return new KeyValuePair<bool, string>(true, Languages.Language.ResourceManager.GetString("相机检索成功") ?? string.Empty);
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        public event EventHandler<CameraFinderItemInfoModel>? CameraBound;

        public DefaultDeviceService(IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IPanoramaCameraConfigRepository panoramaCameraConfigRepository,
            IVolumeCameraConfigRepository volumeCameraConfigRepository,
            IConfigRepository configRepository, IDynamicScale dynamicScale,
            IStaticScale staticScale, IOcr ocr,
            IUsbCameraConfigRepository usbCameraConfigRepository) {
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _panoramaCameraConfigRepository = panoramaCameraConfigRepository;
            _volumeCameraConfigRepository = volumeCameraConfigRepository;
            _configRepository = configRepository;
            _dynamicScale = dynamicScale;
            _staticScale = staticScale;
            _ocr = ocr;
            _usbCameraConfigRepository = usbCameraConfigRepository;
            //注册磅秤事件
            _dynamicScale.StabledWeight += delegate (object? sender, float f) {
                OnStableWeight(new StableWeightEventArgs() {
                    Scale = (IScale?)sender,
                    Weight = f
                });
            };
            _dynamicScale.WeightStabilized += delegate (object? sender, WeightChangedEventArgs args) {
                OnWeightStabilized(args);
            };
            _dynamicScale.Connected += delegate (object? sender, IScale scale) {
                OnScaleConnected(new ScaleConnectedEventArgs() {
                    ConnectionParameters = new BaseScaleConnectParam() {
                        BaudRate = _weightSettingsDto?.Connection?.BaudRate ?? 0,
                        DataBits = _weightSettingsDto?.Connection?.DataBits ?? 0,
                        Parity = _weightSettingsDto?.Connection?.Parity ?? 0,
                        PortName = _weightSettingsDto?.Connection?.PortName ?? string.Empty,
                        StopBits = _weightSettingsDto?.Connection?.StopBits ?? 0
                    },
                    ScaleType = ScaleType.Dynamic
                });
            };
            _dynamicScale.Excepted += delegate (object? sender, Exception exception) {
                OnDeviceException(new DeviceExceptionEventArgs() {
                    ExceptionMessage = exception
                });
            };
            _staticScale.StabledWeight += delegate (object? sender, float f) {
                OnStableWeight(new StableWeightEventArgs() {
                    Scale = (IScale?)sender,
                    Weight = f
                });
            };
            _staticScale.Excepted += delegate (object? sender, Exception exception) {
                //异常的输出之后需要取消
                OnDeviceException(new DeviceExceptionEventArgs() {
                    ExceptionMessage = exception
                });
            };
            _staticScale.WeightStabilized += delegate (object? sender, WeightChangedEventArgs args) {
                OnWeightStabilized(args);
            };
            _staticScale.WeightCleared += (sender, args) => {
                OnWeightCleared(args);
            };
            _staticScale.Connected += delegate (object? sender, IScale scale) {
                OnScaleConnected(new ScaleConnectedEventArgs() {
                    ConnectionParameters = new BaseScaleConnectParam() {
                        BaudRate = _weightSettingsDto?.Connection?.BaudRate ?? 0,
                        DataBits = _weightSettingsDto?.Connection?.DataBits ?? 0,
                        Parity = _weightSettingsDto?.Connection?.Parity ?? 0,
                        PortName = _weightSettingsDto?.Connection?.PortName ?? string.Empty,
                        StopBits = _weightSettingsDto?.Connection?.StopBits ?? 0
                    },
                    ScaleType = ScaleType.Static
                });
            };
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async settings => {
                if (settings is SettingsChangedEvent { SettingsName: "BarcodeFilterSettings" }) {
                    _barcodeFilterSettingsDto = await _configRepository.FirstOrDefaultEntity<BarcodeFilterSettingsDto>("BarcodeFilterSettings") ??
                        new BarcodeFilterSettingsDto();

                    if (RunningStatus) {
                        OnDeviceException(new DeviceExceptionEventArgs() {
                            ExceptionMessage = new Exception($"{Languages.Language.ResourceManager.GetString("必须先停止运行再设置条码过滤才能生效") ?? string.Empty}")
                        });
                    }
                }
                else if (settings is SettingsChangedEvent { SettingsName: "CameraSdkSelector" }) {
                    try {
                        var configInfoModel = await _configRepository.FirstOrDefault(f =>
                            f.ConfigName.Equals("CameraSdkSelector"));
                        _cameraSdkSelectorDto = configInfoModel is not null ? JsonConvert.DeserializeObject<CameraSdkSelectorDto>(configInfoModel.Value) : new CameraSdkSelectorDto();
                    }
                    catch (Exception e) {
                        OnDeviceException(new DeviceExceptionEventArgs() {
                            ExceptionMessage = new Exception($"{e.Message}")
                        });
                    }
                }
            });
        }

        public async Task<KeyValuePair<bool, string>> OnCameraBound(CameraFinderItemInfoModel camera, CancellationToken token = default) {
            await Task.Yield();
            if (RunningStatus) {
                return new KeyValuePair<bool, string>(false, $"{Languages.Language.ResourceManager.GetString("设备运行中则不能解绑或者绑定") ?? string.Empty}");
            }
            else {
                CameraBound?.Invoke(null, camera);
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
        }

        public event EventHandler<CameraFinderItemInfoModel>? CameraUnbound;

        public async Task<KeyValuePair<bool, string>> OnCameraUnbound(CameraFinderItemInfoModel camera, CancellationToken token = default) {
            await Task.Yield();
            if (RunningStatus) {
                return new KeyValuePair<bool, string>(false, $"{Languages.Language.ResourceManager.GetString("设备运行中则不能解绑或者绑定") ?? string.Empty}");
            }
            else {
                CameraUnbound?.Invoke(null, camera);
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
        }

        public event EventHandler<List<CameraParametersModifiedEventArgs>>? CameraParametersModified;

        public async Task<KeyValuePair<bool, string>> OnCameraParametersModified(List<CameraParametersModifiedEventArgs> camera, CancellationToken token = default) {
            await Task.Yield();
            if (RunningStatus) {
                return new KeyValuePair<bool, string>(false, $"{Languages.Language.ResourceManager.GetString("设备运行中则不能解绑或者绑定") ?? string.Empty}");
            }
            else {
                _cameras.Clear();
                var scannerCameraConfigInfoModels = await _barcodeScannerCameraConfigRepository.Select(s => s.Id > 0, o => o.Id, token);
                var panoramaCameraConfigInfoModels = await _panoramaCameraConfigRepository.Select(s => s.Id > 0, o => o.Id, token);
                var volumeCameraConfigInfoModels = await _volumeCameraConfigRepository.Select(s => s.Id > 0, o => o.Id, token);
                //保存绑定参数
                _cameraParameters.Clear();
                _cameraParameters.AddRange(scannerCameraConfigInfoModels?.Select(s => new CameraParametersModifiedEventArgs {
                    Type = CameraBindingType.ScannerCamera,
                    Parameters = s
                })?.ToList() ?? new List<CameraParametersModifiedEventArgs>());
                _cameraParameters.AddRange(panoramaCameraConfigInfoModels?.Select(s => new CameraParametersModifiedEventArgs {
                    Type = CameraBindingType.PanoramaCamera,
                    Parameters = s
                })?.ToList() ?? new List<CameraParametersModifiedEventArgs>());
                _cameraParameters.AddRange(volumeCameraConfigInfoModels?.Select(s => new CameraParametersModifiedEventArgs {
                    Type = CameraBindingType.VolumeCamera,
                    Parameters = s
                })?.ToList() ?? new List<CameraParametersModifiedEventArgs>());

                CameraParametersModified?.Invoke(null, camera);
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
        }

        public event EventHandler<string>? CameraReleased;

        public event EventHandler<ScaleConnectedEventArgs>? ScaleConnected;

        public event EventHandler<OcrExceptionEventArgs>? OcrExceptionOccurred;

        public event EventHandler<OcrInitializationExceptionEventArgs>? OcrInitializationExceptionOccurred;

        public event EventHandler<OcrResult>? OcrContentRecognized;

        public event EventHandler<AuthenticationExceptionEventArgs>? AuthenticationExceptionOccurred;

        public event EventHandler<ScaleDisconnectedEventArgs>? ScaleDisconnected;

        public event EventHandler<RealTimeWeightEventArgs>? RealTimeWeight;

        public event EventHandler<StableWeightEventArgs>? StableWeight;

        public event EventHandler<WeightChangedEventArgs>? WeightStabilized;

        public event EventHandler<WeightChangedEventArgs>? WeightCleared;

        public event EventHandler<DeviceExceptionEventArgs>? DeviceException;

        public async Task<KeyValuePair<bool, string>> Start(CancellationToken token = default) {
            //在这里初始化
            await Initialization();
            //启动(逐个相机启动)
            foreach (var camera in _cameras.OrderByDescending(o => o.BindingType)) {
                //设置过滤
                if (camera.BindingType is CameraBindingType.ScannerCamera or CameraBindingType.OcrCamera) {
                    if (camera is IIndustrialCamera industrialCamera) {
                        industrialCamera.SetScanCodeFilterParams(new ScanCodeFilterParams() {
                            DuplicateBarcodeFilterCount = _barcodeFilterSettingsDto?.DuplicateBarcodeFilterCount ?? 0,
                            RegularExpression = _barcodeFilterSettingsDto?.BasicFilterInfo?.RegularExpression ?? string.Empty,
                            ScanInterval = _barcodeFilterSettingsDto?.ScanInterval ?? 1000,
                            FilterOutContent = _barcodeFilterSettingsDto?.FilterOutputType switch {
                                FilterOutputType.NoRead => "NoRead",
                                FilterOutputType.Filtered => "Filtered",
                                _ => string.Empty
                            },
                            BarCodeFilterMode = (BarCodeFilterMode)(_barcodeFilterSettingsDto?.BarCodeFilterOptions ?? BarCodeFilterOptions.None),
                            IsUseCustomRegexReplacement = _barcodeFilterSettingsDto?.IsUseCustomRegexReplacement ?? false,
                            CustomRegexReplacementItems = _barcodeFilterSettingsDto?.CustomRegexReplacementItems?.Where(w => w.IsActive)?.Select(s =>
                                new CustomRegexReplacementItemInfo {
                                    RegexPattern = s.RegexPattern,
                                    ReplaceContent = s.ReplaceContent
                                })?.ToList() ?? new List<CustomRegexReplacementItemInfo>(),
                            CustomRegularExpressionItems = _barcodeFilterSettingsDto?.CustomRegexFilterItems?.Where(w => w.IsActive)?
                                .Select(s => s.RegexPattern)?.ToList() ?? new List<string>()
                        });
                    }
                    else if (camera is ISmartCamera smartCamera) {
                        smartCamera.SetScanCodeFilterParams(new ScanCodeFilterParams() {
                            DuplicateBarcodeFilterCount = _barcodeFilterSettingsDto?.DuplicateBarcodeFilterCount ?? 0,
                            RegularExpression = _barcodeFilterSettingsDto?.BasicFilterInfo?.RegularExpression ?? string.Empty,
                            ScanInterval = _barcodeFilterSettingsDto?.ScanInterval ?? 1000,
                            FilterOutContent = _barcodeFilterSettingsDto?.FilterOutputType switch {
                                FilterOutputType.NoRead => "NoRead",
                                FilterOutputType.Filtered => "Filtered",
                                _ => string.Empty
                            },
                            BarCodeFilterMode = (BarCodeFilterMode)(_barcodeFilterSettingsDto?.BarCodeFilterOptions ?? BarCodeFilterOptions.None),
                            IsUseCustomRegexReplacement = _barcodeFilterSettingsDto?.IsUseCustomRegexReplacement ?? false,
                            CustomRegexReplacementItems = _barcodeFilterSettingsDto?.CustomRegexReplacementItems?.Where(w => w.IsActive)?.Select(s =>
                                new CustomRegexReplacementItemInfo {
                                    RegexPattern = s.RegexPattern,
                                    ReplaceContent = s.ReplaceContent
                                })?.ToList() ?? new List<CustomRegexReplacementItemInfo>(),
                            CustomRegularExpressionItems = _barcodeFilterSettingsDto?.CustomRegexFilterItems?.Where(w => w.IsActive)?
                                .Select(s => s.RegexPattern)?.ToList() ?? new List<string>()
                        });
                    }
                }

                await Task.Delay(100, token);
                await camera.Start(string.Empty);
                if (_cameraSdkSelectorDto?.IsUsbCameraSdk == true) {
                    //判断是否Usb相机或者安防相机扫码
                    if (camera is NormalUsbCamera usbCamera) {
                        var usbCameraParameter = await GetUsbCameraParameter(usbCamera.Info?.SerialNumber ?? string.Empty);
                        var barcodeReaderParameter = await GetBarcodeReaderParameter();
                        var dictionary = new Dictionary<string, object>();
                        if (usbCameraParameter is not null) {
                            dictionary.Add("UsbCameraParameter", usbCameraParameter);
                        }

                        if (barcodeReaderParameter is not null) {
                            dictionary.Add("BarcodeReaderParameter", barcodeReaderParameter);
                        }
                        usbCamera.SetParameters(dictionary);
                    }
                }
            }
            //连接磅秤
            if (_weightSettingsDto is not null) {
                switch (_weightSettingsDto.Mode) {
                    case WeightMode.Static:
                        _staticScale.Connect(new BaseScaleConnectParam() {
                            PortName = _weightSettingsDto.Connection.PortName,
                            BaudRate = _weightSettingsDto.Connection.BaudRate,
                            DataBits = _weightSettingsDto.Connection.DataBits,
                            Parity = _weightSettingsDto.Connection.Parity,
                            StopBits = _weightSettingsDto.Connection.StopBits
                        });
                        //连接静态称
                        break;

                    case WeightMode.Dynamic:
                        _dynamicScale.Connect(new BaseScaleConnectParam() {
                            PortName = _weightSettingsDto.Connection.PortName,
                            BaudRate = _weightSettingsDto.Connection.BaudRate,
                            DataBits = _weightSettingsDto.Connection.DataBits,
                            Parity = _weightSettingsDto.Connection.Parity,
                            StopBits = _weightSettingsDto.Connection.StopBits
                        });
                        break;
                }
            }

            return new KeyValuePair<bool, string>(false, string.Empty);
        }

        public async Task<KeyValuePair<bool, string>> Stop(CancellationToken token = default) {
            await Task.Yield();
            Dispose();
            RunningStatus = false;
            return new KeyValuePair<bool, string>(true, string.Empty);
        }

        public async Task Initialization() {
            await Task.Yield();
            if (RunningStatus) {
                return;
            }

            //如果没有枚举过相机就需要在这里枚举
            if (_cameraInfos.Count == 0) {
                await OnCameraEnumerationRefreshed();
            }

            await Task.Run(async () => {
                OcrSettingsDto ocrSettingsDto = new();
                try {
                    _barcodeFilterSettingsDto = await _configRepository.FirstOrDefaultEntity<BarcodeFilterSettingsDto>("BarcodeFilterSettings") ??
                                                new BarcodeFilterSettingsDto();

                    try {
                        ocrSettingsDto = await _configRepository.FirstOrDefaultEntity<OcrSettingsDto>("OcrSettings") ??
                            new OcrSettingsDto();
                        var modelFilePath = ocrSettingsDto.ModelFilePath;
                        if (string.IsNullOrEmpty(modelFilePath)) {
                            if (!Directory.Exists($"{System.AppDomain.CurrentDomain.BaseDirectory}OnnxModels")) {
                                Directory.CreateDirectory($"{System.AppDomain.CurrentDomain.BaseDirectory}OnnxModels");
                            }

                            modelFilePath = Directory.GetFiles($"{System.AppDomain.CurrentDomain.BaseDirectory}OnnxModels")
                                ?.Select(name => new FileInfo(name))?.FirstOrDefault(f => f.Extension.Contains("onnx"))?.FullName ?? string.Empty;
                        }
                        else {
                            modelFilePath = Directory.GetFiles($"{System.AppDomain.CurrentDomain.BaseDirectory}OnnxModels")
                                ?.Select(name => new FileInfo(name))?.FirstOrDefault(f => f.Name.Equals(modelFilePath))?.FullName ?? string.Empty;
                        }
                        await _ocr.SetOnnxModelPath(modelFilePath);
                        await _ocr.SetConfidenceThreshold(ocrSettingsDto.ConfidenceThreshold);
                        await _ocr.SetRectangleScale(ocrSettingsDto.RectangleScale);
                        await _ocr.SetIsSecondConfirmationEnabled(ocrSettingsDto.IsSecondConfirmationEnabled);
                    }
                    catch (Exception e) {
                        OnDeviceException(new DeviceExceptionEventArgs() {
                            ExceptionMessage = new Exception($"加载Ocr设置识别")
                        });
                    }
                    _cameras.Clear();
                    var scannerCameraConfigInfoModels = await _barcodeScannerCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                    var panoramaCameraConfigInfoModels = await _panoramaCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                    var volumeCameraConfigInfoModels = await _volumeCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);

                    _cameraParameters.Clear();
                    _cameraParameters.AddRange(scannerCameraConfigInfoModels?.Select(s => new CameraParametersModifiedEventArgs {
                        Type = CameraBindingType.ScannerCamera,
                        Parameters = s
                    })?.ToList() ?? new List<CameraParametersModifiedEventArgs>());
                    _cameraParameters.AddRange(panoramaCameraConfigInfoModels?.Select(s => new CameraParametersModifiedEventArgs {
                        Type = CameraBindingType.PanoramaCamera,
                        Parameters = s
                    })?.ToList() ?? new List<CameraParametersModifiedEventArgs>());
                    _cameraParameters.AddRange(volumeCameraConfigInfoModels?.Select(s => new CameraParametersModifiedEventArgs {
                        Type = CameraBindingType.VolumeCamera,
                        Parameters = s
                    })?.ToList() ?? new List<CameraParametersModifiedEventArgs>());

                    //初始化已经绑定的相机

                    foreach (var parameter in _cameraParameters) {
                        ICamera? camera = null;
                        //创建对象
                        switch (parameter.Type) {
                            case CameraBindingType.ScannerCamera: {
                                    //扫码相机
                                    if (parameter.Parameters is BarcodeScannerCameraConfigInfoModel model) {
                                        var tryGetValue = _cameraInfos.TryGetValue(model.SerialNumber, out var info);
                                        if (tryGetValue && info is not null) {
                                            //转换绑定
                                            camera = ConvertCamera(info);
                                            if (camera is not null) {
                                                //设置绑定模式
                                                //判断是否使用Ocr
                                                camera.BindingType = model.IsOcrSupported ? CameraBindingType.OcrCamera : CameraBindingType.ScannerCamera;
                                                if (camera.BindingType == CameraBindingType.OcrCamera &&
                                                    !ocrSettingsDto.IsUseOcr) {
                                                    camera = null;
                                                }
                                            }
                                        }
                                    }

                                    break;
                                }
                            case CameraBindingType.PanoramaCamera: {
                                    //全景相机
                                    if (parameter.Parameters is PanoramaCameraConfigInfoModel model) {
                                        var tryGetValue = _cameraInfos.TryGetValue(model.SerialNumber, out var info);
                                        if (tryGetValue && info is not null) {
                                            //转换绑定
                                            camera = ConvertCamera(info);
                                            if (camera is not null) {
                                                //设置绑定模式
                                                camera.BindingType = CameraBindingType.PanoramaCamera;
                                                camera.Info.Type = (CameraType)model.CameraType;
                                            }
                                        }
                                    }

                                    break;
                                }
                            case CameraBindingType.VolumeCamera: {
                                    //体积相机
                                    if (parameter.Parameters is VolumeCameraConfigInfoModel model) {
                                        var tryGetValue = _cameraInfos.TryGetValue(model.SerialNumber, out var info);
                                        if (tryGetValue && info is not null) {
                                            //转换绑定
                                            camera = ConvertCamera(info);
                                            if (camera is not null) {
                                                //设置绑定模式
                                                camera.BindingType = CameraBindingType.VolumeCamera;
                                            }
                                        }
                                    }

                                    break;
                                }
                        }

                        if (camera is not null) {
                            //注册事件

                            camera.CameraDisconnected += delegate (object? sender, CameraConnectionEventArgs args) {
                                if (sender is ICamera mCamera) {
                                    OnCameraDisconnected(mCamera);
                                }
                            };
                            camera.CameraExceptionOccurred += delegate (object? sender, CameraExceptionEventArgs args) {
                                var mCameraInfo = string.Empty;
                                if (sender is ICamera mCamera) {
                                    mCameraInfo =
                                        $"ID:{mCamera.Info?.Id},SerialNumber:{mCamera?.Info?.SerialNumber},SdkType:{mCamera?.SdkType}";
                                }
                                OnCameraException(new DeviceExceptionEventArgs() {
                                    ExceptionMessage = new Exception($"{args.Exception?.Message}")
                                });
                                OnDeviceException(new DeviceExceptionEventArgs() {
                                    ExceptionMessage = new Exception($"{mCameraInfo}-{args.Exception?.Message}")
                                });
                            };
                            camera.PhotoTaken += delegate (object? sender, PhotoTakenEventArgs args) {
                                OnPanoramaCaptured(new PanoramaCaptureEventArgs() {
                                    CameraSerialNumber = args.CameraSerialNumber,
                                    Image = args.Image,
                                    PhotoTime = args.PhotoTime,
                                    Timestamp = args.Timestamp,
                                    ThumbImage = args.ThumbImage,
                                    Barcode = args.Barcode,
                                    BarcodeTimestamp = args.BarcodeTimestamp
                                });
                            };
                            camera.RealtimeImage += delegate (object? sender, RealtimeImageEventArgs args) {
                                OnRealTimeImage(new RealTimeImageEventArgs() {
                                    Camera = camera,
                                    Image = args.ThumbImage,
                                });
                            };
                            //判断相机类型(各自注册事件)

                            switch (camera) {
                                case IIndustrialCamera industrialCamera:
                                    industrialCamera.TakePhotoDelay = panoramaCameraConfigInfoModels
                                        ?.FirstOrDefault(f => f.SerialNumber.Equals(camera.Info?.SerialNumber))
                                        ?.CaptureDelayTime ?? 0;
                                    //填充其他信息
                                    industrialCamera.BarcodeRead += delegate (object? sender, BarcodeReadEventArgs args) {
                                        OnBarcodeScanned(args);
                                    };
                                    industrialCamera.OcrContentRecognized += delegate (object? sender,
                                        OcrResult args) {
                                            OnOcrContentRecognized(args);
                                        };
                                    var isShowRealTimeImage = scannerCameraConfigInfoModels?.FirstOrDefault(f =>
                                        f.SerialNumber.Equals(camera.Info?.SerialNumber))
                                        ?.IsShowRealTimeImage;
                                    if (isShowRealTimeImage == true) {
                                        industrialCamera.StartRealTimeImage();
                                    }

                                    if (industrialCamera.BindingType == CameraBindingType.OcrCamera) {
                                        industrialCamera.Ocr = _ocr;
                                    }
                                    break;

                                case ISmartCamera smartCamera:
                                    smartCamera.BarcodeReadTriggered +=
                                        delegate (object? sender, BarcodeTriggeredEventArgs args) {
                                            OnBarcodeScanned(args);
                                        };
                                    smartCamera.NotBarcodeHitEvent += delegate (object? sender, BarcodeReadEventArgs args) {
                                        OnNotBarcodeHitEvent(args);
                                    };
                                    smartCamera.OcrContentRecognized += delegate (object? sender,
                                        OcrResult args) {
                                            OnOcrContentRecognized(args);
                                        };
                                    try {
                                        var scannerCameraConfigInfoModel = scannerCameraConfigInfoModels?.FirstOrDefault(f =>
                                            f.SerialNumber.Equals(camera.Info?.SerialNumber));
                                        var parameters = scannerCameraConfigInfoModel?.CameraConnectionParameters;
                                        if (!string.IsNullOrEmpty(parameters)) {
                                            var jObject = JObject.Parse(parameters);
                                            if (jObject["TriggerMode"] is not null) {
                                                smartCamera.TriggerMode = (TriggerMode)(jObject["TriggerMode"] ?? 0).Value<int>();
                                            }

                                            if (jObject["SourceLine"] is not null) {
                                                smartCamera.SourceLine = (jObject["SourceLine"] ?? 0).Value<int>();
                                            }
                                        }

                                        if (scannerCameraConfigInfoModel?.IsShowRealTimeImage == true) {
                                            smartCamera.StartRealTimeImage();
                                        }
                                        if (smartCamera.BindingType == CameraBindingType.OcrCamera) {
                                            smartCamera.Ocr = _ocr;
                                        }
                                    }
                                    catch (Exception e) {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                                    }

                                    break;

                                case ISecurityCamera securityCamera: {
                                        var parameters = panoramaCameraConfigInfoModels
                                            ?.FirstOrDefault(f => f.SerialNumber.Equals(camera.Info?.SerialNumber))
                                            ?.CameraConnectionParameters;
                                        securityCamera.CameraConnectionParameters =
                                            parameters ?? string.Empty;
                                        securityCamera.BarcodeRead += (sender, args) => {
                                            OnBarcodeScanned(args);
                                        };

                                        securityCamera.OcrContentRecognized += delegate (object? sender,
                                            OcrResult args) {
                                                OnOcrContentRecognized(args);
                                            };
                                        break;
                                    }
                                case IVolumeCamera volumeCamera: {
                                        volumeCamera.VolumeCaptured += delegate (object? sender,
                                            VolumeCapturedEventArgs args) {
                                                OnVolumeCaptured(args);
                                            };
                                        break;
                                    }
                            }

                            //初始化
                            var (b, s) = await camera.Initialize(camera?.Info);
                            if (!b) {
                                OnDeviceException(new DeviceExceptionEventArgs() {
                                    ExceptionMessage = new Exception(s)
                                });
                            }

                            //添加到集合
                            _cameras.Add(camera);
                        }
                    }
                    OnCameraInitialized(_cameras);
                    //磅秤相关
                    //获取磅秤配置
                    var infoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("WeightSettings"));
                    if (infoModel is not null) {
                        try {
                            _staticScale.Dispose();
                            _dynamicScale.Dispose();
                            await Task.Delay(TimeSpan.FromSeconds(1));
                            _weightSettingsDto = JsonConvert.DeserializeObject<WeightSettingsDto>(infoModel.Value);
                            if (_weightSettingsDto is not null) {
                                //判断需要连接的磅秤
                                var properties = new WeightAdditionalProperties() {
                                    IsUseActualWeightConversionRate =
                           _weightSettingsDto.AdditionalWeight.IsUseActualWeightConversionRate,
                                    IsUseAppendedWeight = _weightSettingsDto.AdditionalWeight.IsUseAppendedWeight,
                                    IsUseFixedWeight = _weightSettingsDto.AdditionalWeight.IsUseFixedWeight,
                                    IsUseMergedWeightTimeout = _weightSettingsDto.AdditionalWeight.IsUseMergedWeightTimeout,
                                    WeightConversionRate = _weightSettingsDto.AdditionalWeight.WeightConversionRate,
                                    AppendedWeightValue = _weightSettingsDto.AdditionalWeight.AppendedWeightValue,
                                    FixedWeightValue = _weightSettingsDto.AdditionalWeight.FixedWeightValue,
                                    MergedWeightTimeout = _weightSettingsDto.AdditionalWeight.MergedWeightTimeout
                                };
                                switch (_weightSettingsDto.Mode) {
                                    //连接
                                    case WeightMode.Static:
                                        ScaleType = ScaleType.Static;
                                        _staticScale.WeightFormat = (ScaleWeightFormat)_weightSettingsDto.Connection.DataFormat;
                                        _staticScale.WeightAdditionalProperties = properties;
                                        _staticScale.SetWeightCalculationParameters(new DefaultStaticScaleValueParameters() {
                                            AccessMode = (Plugin.Scale.StaticScale.WeightAccessMode)_weightSettingsDto.StaticWeight.AccessMode,
                                            BalanceCount = _weightSettingsDto.StaticWeight.BalanceCount,
                                            BalanceQty = _weightSettingsDto.StaticWeight.BalanceQty,
                                            CharacterLength = _weightSettingsDto.StaticWeight.CharacterLength,
                                            DataInterval = _weightSettingsDto.StaticWeight.DataInterval,
                                            DecimalEndPosition = _weightSettingsDto.StaticWeight.DecimalEndPosition,
                                            DecimalStartPosition = _weightSettingsDto.StaticWeight.DecimalStartPosition,
                                            Identifier = _weightSettingsDto.StaticWeight.Identifier,
                                            IdentifierPosition = _weightSettingsDto.StaticWeight.IdentifierPosition,
                                            IntegerEndPosition = _weightSettingsDto.StaticWeight.IntegerEndPosition,
                                            IntegerStartPosition = _weightSettingsDto.StaticWeight.IntegerStartPosition,
                                            IsReversed = _weightSettingsDto.StaticWeight.IsReversed,
                                            SendingContent = _weightSettingsDto.StaticWeight.SendingContent,
                                            SendingFormat = (ScaleWeightFormat)_weightSettingsDto.StaticWeight.SendingFormat,
                                            MaxWeight = _weightSettingsDto.CommonWeight.MaxWeight,
                                            MinWeight = _weightSettingsDto.CommonWeight.MinWeight
                                        });

                                        break;

                                    case WeightMode.Dynamic:
                                        //连接动态称
                                        ScaleType = ScaleType.Dynamic;
                                        _dynamicScale.WeightFormat = (ScaleWeightFormat)_weightSettingsDto.Connection.DataFormat;
                                        _dynamicScale.WeightAdditionalProperties = properties;
                                        _dynamicScale.SetWeightCalculationParameters(new DefaultDynamicScaleValueParameters() {
                                            DecimalPlaces = _weightSettingsDto.DynamicWeight.DecimalPrecision
                                        });

                                        break;

                                    case WeightMode.None:
                                        ScaleType = ScaleType.None;
                                        break;
                                }
                            }
                        }
                        catch (Exception e) {
                            OnDeviceException(new DeviceExceptionEventArgs() {
                                ExceptionMessage = new Exception($"{Languages.Language.ResourceManager.GetString("加载磅秤设置失败") ?? string.Empty}:{e.Message}")
                            });
                        }
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }
            });
        }

        public void Dispose() {
            try {
                for (int i = _cameras.Count - 1; i >= 0; i--) {
                    var serialNumber = _cameras[i]?.Info?.SerialNumber ?? string.Empty;
                    _cameras[i]?.Dispose();
                    OnCameraReleased(serialNumber);
                }
                _dynamicScale?.Dispose();
                _staticScale?.Dispose();
            }
            catch (Exception e) {
                OnDeviceException(new DeviceExceptionEventArgs() {
                    ExceptionMessage = new Exception($"{Languages.Language.ResourceManager.GetString("释放设备异常") ?? string.Empty}:{e.Message}")
                });
                NLog.LogManager.GetCurrentClassLogger().Error(JsonConvert.SerializeObject(e));
            }
        }

        protected virtual async void OnCameraInitialized(List<ICamera> e) {
            await Task.Yield();
            RunningStatus = true;
            CameraInitialized?.Invoke(this, e);
        }

        protected virtual async void OnCameraDisconnected(ICamera e) {
            await _cameraSlim.WaitAsync();
            _cameras.Remove(e);
            _cameraSlim.Release();
            CameraDisconnected?.Invoke(this, _cameras);
        }

        protected virtual async void OnCameraFault(List<ICamera> e) {
            await Task.Yield();
            CameraFault?.Invoke(this, e);
        }

        protected virtual async void OnBarcodeScanned(BarcodeReadEventArgs e) {
            await Task.Yield();
            BarcodeScanned?.Invoke(this, e);
        }

        protected virtual async void OnNotBarcodeHitEvent(BarcodeReadEventArgs e) {
            await Task.Yield();
            NotBarcodeHitEvent?.Invoke(this, e);
        }

        protected virtual async void OnPanoramaCaptured(PanoramaCaptureEventArgs e) {
            await Task.Yield();
            PanoramaCaptured?.Invoke(this, e);
        }

        protected virtual async void OnVolumeCaptured(VolumeCapturedEventArgs e) {
            await Task.Yield();
            VolumeCaptured?.Invoke(this, e);
        }

        protected virtual async void OnRealTimeImage(RealTimeImageEventArgs e) {
            await Task.Yield();
            RealTimeImage?.Invoke(this, e);
        }

        protected virtual async void OnCameraBound(CameraFinderItemInfoModel e) {
            await Task.Yield();
            CameraBound?.Invoke(this, e);
        }

        protected virtual async void OnCameraParametersModified(List<CameraParametersModifiedEventArgs> e) {
            await Task.Yield();
            CameraParametersModified?.Invoke(this, e);
        }

        protected virtual async void OnDeviceException(DeviceExceptionEventArgs e) {
            await Task.Yield();
            DeviceException?.Invoke(this, e);
        }

        protected virtual async void OnCameraReleased(string e) {
            await Task.Yield();
            CameraReleased?.Invoke(this, e);
        }

        private CameraType ConvertCameraType(string brand, string modelName) {
            switch (brand) {
                case not null when (brand.Contains("Hikrobot") || brand.Contains("Hikvision")):
                    if (modelName.Contains("MV-D")) {
                        return CameraType.ThreeDCamera;
                    }
                    if (modelName.Contains("MV-ID"))
                        return CameraType.SmartCamera;
                    if (modelName.Contains("MV-PD"))
                        return CameraType.IndustrialCamera;
                    break;

                case not null when (brand.Contains("Dahua") || brand.Contains("Huaray")):
                    if (modelName.Contains("IPC"))
                        return CameraType.VideoCamera;
                    if (modelName.Contains("DH-MV-S") || modelName.Contains("DH-MV-R") || modelName.Contains("DH-SL")
                        || modelName.StartsWith("R") || modelName.StartsWith("S5"))
                        return CameraType.SmartCamera;
                    if (modelName.Contains("DH-MV-D"))
                        return CameraType.ThreeDCamera;
                    break;

                case not null when (brand.Contains("Wayzim") /*|| info.Brand.Contains("Huaray")*/):
                    if (modelName.Contains("SmartCamera"))
                        return CameraType.SmartCamera;
                    if (modelName.Contains("IndustrialCamera"))
                        return CameraType.IndustrialCamera;
                    break;

                case not null when (brand.Contains("量方") /*|| info.Brand.Contains("Huaray")*/):
                    if (modelName.Contains("Orbbec"))
                        return CameraType.ThreeDCamera;
                    break;

                case not null when (brand.Contains("Microsoft") /*|| info.Brand.Contains("Huaray")*/):
                    return CameraType.IndustrialCamera;

                default:
                    return CameraType.IndustrialCamera;
            }
            return CameraType.IndustrialCamera;
        }

        private ICamera? ConvertCamera(CameraInfo info) {
            switch (info.Brand) {
                case not null when (info.Brand.Contains("Hikrobot") || info.Brand.Contains("Hikvision")):
                    if (info.Model.Contains("MV-D")) {
                        return new HikvisionVolumeCamera(info);
                    }
                    if (info.Model.Contains("MV-ID"))
                        return new HikvisionSmartCamera(info);
                    if (info.Model.Contains("MV-PD"))
                        return new HikvisionIndustrialCamera(info);
                    break;

                case not null when (info.Brand.Contains("Dahua") || info.Brand.Contains("Huaray")):
                    if (info.Model.Contains("IPC"))
                        return new DaHuatechSecurityCamera(info);
                    if (info.Model.Contains("DH-MV-S") || info.Model.Contains("DH-MV-R")
                                                       || info.Model.Contains("DH-SL") ||
                                                       info.Model.StartsWith("R") || info.Model.Contains("S5"))
                        return new DaHuaSmartCamera(info);
                    if (info.Model.Contains("DH-MV-D"))
                        return new DaHuaVolumeCamera(info);
                    break;

                case not null when (info.Brand.Contains("Wayzim") /*|| info.Brand.Contains("Huaray")*/):
                    if (info.Model.Contains("SmartCamera"))
                        return new WayzimSmartCamera(info);
                    if (info.Model.Contains("IndustrialCamera"))
                        return new WayzimIndustrialCamera(info);
                    break;

                case not null when (info.Brand.Contains("量方") /*|| info.Brand.Contains("Huaray")*/):
                    if (info.Model.Contains("Orbbec"))
                        return new DimensionVolumeCamera(info);
                    break;

                case not null when (info.Brand.Contains("Microsoft") /*|| info.Brand.Contains("Huaray")*/):
                    return new NormalUsbCamera(info);

                default:
                    return null;
            }
            return null;
        }

        private async Task<Dictionary<UsbCameraParameter, object>?> GetUsbCameraParameter(string serialNumber) {
            try {
                var usbCameraConfigInfoModel = await _usbCameraConfigRepository.
                    FirstOrDefault(f =>
                        f.SerialNumber.Equals(serialNumber));
                if (usbCameraConfigInfoModel is not null) {
                    var dictionary = new Dictionary<UsbCameraParameter, object>();
                    //曝光度
                    if (usbCameraConfigInfoModel.IsCustomExposureEnabled) {
                        dictionary.Add(UsbCameraParameter.Exposure, usbCameraConfigInfoModel.Exposure);
                    }
                    //亮度
                    if (usbCameraConfigInfoModel.IsCustomBrightnessEnabled) {
                        dictionary.Add(UsbCameraParameter.Brightness, usbCameraConfigInfoModel.Brightness);
                    }
                    //对比度
                    if (usbCameraConfigInfoModel.IsCustomContrastEnabled) {
                        dictionary.Add(UsbCameraParameter.Contrast, usbCameraConfigInfoModel.Contrast);
                    }
                    //色调
                    if (usbCameraConfigInfoModel.IsCustomHueEnabled) {
                        dictionary.Add(UsbCameraParameter.Hue, usbCameraConfigInfoModel.Hue);
                    }
                    //锐度
                    if (usbCameraConfigInfoModel.IsCustomSharpnessEnabled) {
                        dictionary.Add(UsbCameraParameter.Sharpness, usbCameraConfigInfoModel.Sharpness);
                    }
                    //伽马值
                    if (usbCameraConfigInfoModel.IsCustomGammaEnabled) {
                        dictionary.Add(UsbCameraParameter.Gamma, usbCameraConfigInfoModel.Gamma);
                    }
                    //白平衡
                    if (usbCameraConfigInfoModel.IsCustomWhiteBalanceEnabled) {
                        dictionary.Add(UsbCameraParameter.WhiteBalance, usbCameraConfigInfoModel.WhiteBalance);
                    }
                    //背光补偿
                    if (usbCameraConfigInfoModel.IsCustomBacklightCompensationEnabled) {
                        dictionary.Add(UsbCameraParameter.BklightComp, usbCameraConfigInfoModel.BklightComp);
                    }
                    return dictionary;
                }
            }
            catch (Exception e) {
            }

            return null;
        }

        private async Task<Dictionary<BarcodeReaderParameter, object>?> GetBarcodeReaderParameter() {
            var usbBarcodeReaderDto = await _configRepository.FirstOrDefaultEntity<UsbBarcodeReaderDto>("AlgorithmSettings") ??
                                      new UsbBarcodeReaderDto();
            var barcodeMapping = new Dictionary<BarcodeType, EnumBarcodeFormat>
            {
                { BarcodeType.QRCode, EnumBarcodeFormat.BF_QR_CODE },
                { BarcodeType.MicroQR, EnumBarcodeFormat.BF_MICRO_QR },
                { BarcodeType.Code128, EnumBarcodeFormat.BF_CODE_128 },
                { BarcodeType.Code39, EnumBarcodeFormat.BF_CODE_39 },
                { BarcodeType.Code93, EnumBarcodeFormat.BF_CODE_93 },
                { BarcodeType.CodeBar, EnumBarcodeFormat.BF_CODABAR },
                { BarcodeType.EAN13, EnumBarcodeFormat.BF_EAN_13 },
                { BarcodeType.EAN8, EnumBarcodeFormat.BF_EAN_8 },
            };
            var barcodeFormat = barcodeMapping.Where(kvp => (usbBarcodeReaderDto.BarcodeType & kvp.Key) == kvp.Key).Aggregate<KeyValuePair<BarcodeType, EnumBarcodeFormat>, EnumBarcodeFormat>(0, (current, kvp) => current | kvp.Value);

            var dictionary = new Dictionary<BarcodeReaderParameter, object>()
            {
                { BarcodeReaderParameter.EnumBarcodeFormat,barcodeFormat },
                { BarcodeReaderParameter.RecognitionMode,(ScanMode)usbBarcodeReaderDto.RecognitionMode },
                { BarcodeReaderParameter.TextureDetectionSensitivity,usbBarcodeReaderDto.TextureDetectionSensitivity },
                { BarcodeReaderParameter.BinarizationBlockSize,usbBarcodeReaderDto.BinarizationBlockSize },
                { BarcodeReaderParameter.ExpectedBarcodesCount,usbBarcodeReaderDto.ExpectedBarcodesCount },
                { BarcodeReaderParameter.DeblurLevel,usbBarcodeReaderDto.DeblurLevel },
                { BarcodeReaderParameter.LocalizationMode,usbBarcodeReaderDto.LocalizationMode },
                { BarcodeReaderParameter.IsUseTextFilterMode,usbBarcodeReaderDto.IsUseTextFilterMode },
                { BarcodeReaderParameter.IsUseRegionPredetectionMode,usbBarcodeReaderDto.IsUseRegionPredetectionMode },
                { BarcodeReaderParameter.ScaleDownThreshold,usbBarcodeReaderDto.ScaleDownThreshold },
                { BarcodeReaderParameter.GrayscaleTransformationMode,usbBarcodeReaderDto.GrayscaleTransformationMode },
                { BarcodeReaderParameter.ImagePreprocessingMode,usbBarcodeReaderDto.ImagePreprocessingMode },
                { BarcodeReaderParameter.MinResultConfidence,usbBarcodeReaderDto.MinResultConfidence },
                { BarcodeReaderParameter.RecognitionSkipFrames,usbBarcodeReaderDto.RecognitionSkipFrames },
                { BarcodeReaderParameter.ScalePercentage,usbBarcodeReaderDto.ScalePercentage },
            };
            return dictionary;
        }

        protected virtual async void OnScaleConnected(ScaleConnectedEventArgs e) {
            await Task.Yield();
            ScaleConnected?.Invoke(this, e);
        }

        protected virtual async void OnScaleDisconnected(ScaleDisconnectedEventArgs e) {
            await Task.Yield();
            ScaleDisconnected?.Invoke(this, e);
        }

        protected virtual async void OnRealTimeWeight(RealTimeWeightEventArgs e) {
            await Task.Yield();
            RealTimeWeight?.Invoke(this, e);
        }

        protected virtual async void OnStableWeight(StableWeightEventArgs e) {
            await Task.Yield();
            StableWeight?.Invoke(this, e);
        }

        protected virtual async void OnWeightStabilized(WeightChangedEventArgs e) {
            await Task.Yield();
            WeightStabilized?.Invoke(this, e);
        }

        protected virtual async void OnCameraException(DeviceExceptionEventArgs e) {
            await Task.Yield();
            CameraException?.Invoke(this, e);
        }

        protected virtual async void OnOcrExceptionOccurred(OcrExceptionEventArgs e) {
            await Task.Yield();
            OcrExceptionOccurred?.Invoke(this, e);
        }

        protected virtual async void OnOcrInitializationExceptionOccurred(OcrInitializationExceptionEventArgs e) {
            await Task.Yield();
            OcrInitializationExceptionOccurred?.Invoke(this, e);
        }

        protected virtual async void OnOcrContentRecognized(OcrResult e) {
            await Task.Yield();
            OcrContentRecognized?.Invoke(this, e);
        }

        protected virtual async void OnAuthenticationExceptionOccurred(AuthenticationExceptionEventArgs e) {
            await Task.Yield();
            AuthenticationExceptionOccurred?.Invoke(this, e);
        }

        protected virtual async void OnWeightCleared(WeightChangedEventArgs e) {
            await Task.Yield();
            WeightCleared?.Invoke(this, e);
        }
    }
}