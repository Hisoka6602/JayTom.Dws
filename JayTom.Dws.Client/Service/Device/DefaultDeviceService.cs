using JayTom.Dws.Application.Configuration;
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
using JayTom.Dws.Plugin.Tcp;
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
using JayTom.Dws.Plugin.Device.KeyboardDevice;
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
using TcpConnectParam = JayTom.Dws.Plugin.Scale.TcpConnectParam;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;

namespace JayTom.Dws.Client.Service.Device
{

    public class DefaultDeviceService : IDeviceService
    {
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private readonly IPanoramaCameraConfigRepository _panoramaCameraConfigRepository;
        private readonly IVolumeCameraConfigRepository _volumeCameraConfigRepository;
        private readonly ISettingsStore _settingsStore;
        private readonly IDynamicScale _dynamicScale;
        private readonly IStaticScale _staticScale;
        private readonly IOcr _ocr;
        private readonly IUsbCameraConfigRepository _usbCameraConfigRepository;
        private readonly IKeyboardDeviceManager _keyboardDeviceManager;
        private readonly SemaphoreSlim _deviceLifecycleGate = new(1, 1);
        /// <summary>
        /// 相机枚举同步门，防止多个刷新请求并发改写共享相机集合。
        /// </summary>
        private readonly SemaphoreSlim _cameraEnumerationGate = new(1, 1);

        //private List<string> CameraInitializationException { get; set; } = new();
        //private List<CameraInfo> _cameraInfos = new();

        private ICamera[] _cameras = [];
        private readonly List<CameraParametersModifiedEventArgs> _cameraParameters = new();
        private BarcodeFilterSettingsDto? _barcodeFilterSettingsDto = new();
        private WeightSettingsDto? _weightSettingsDto = new();
        /// <summary>
        /// 当前存图配置，用于决定相机是否需要输出原分辨率帧。
        /// </summary>
        private ImageSettingsDto? _imageSettingsDto = new();
        private CameraSdkSelectorDto? _cameraSdkSelectorDto;
        private static readonly ConcurrentDictionary<string, CameraInfo> _cameraInfos = new();
        private int _runningStatus;

        public bool RunningStatus => Volatile.Read(ref _runningStatus) != 0;
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

        public async Task<KeyValuePair<bool, string>> OnCameraEnumerationRefreshed(CancellationToken token = default)
        {
            await _cameraEnumerationGate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                _cameraSdkSelectorDto = await _settingsStore.GetAsync<CameraSdkSelectorDto>("CameraSdkSelector", token) ??
                                        new CameraSdkSelectorDto();
                var selector = _cameraSdkSelectorDto;
                var emptyCameraTask = Task.FromResult<List<CameraInfo>?>([]);
                var daHuaSmartTask = selector.IsUseDaHuaSmartCameraSdk
                    ? new DaHuaSmartCamera().EnumerateCameras()
                    : emptyCameraTask;
                var hikvisionIndustrialTask = selector.IsUseHikvisionIndustrialCameraSdk
                    ? new HikvisionIndustrialCamera().EnumerateCameras()
                    : emptyCameraTask;
                var hikvisionSmartTask = selector.IsUseHikvisionSmartCameraSdk
                    ? new HikvisionSmartCamera().EnumerateCameras()
                    : emptyCameraTask;
                var daHuaSecurityTask = selector.IsUseDaHuaSecurityCameraSdk
                    ? new DaHuatechSecurityCamera().EnumerateCameras()
                    : emptyCameraTask;
                var wayzimSmartTask = selector.IsUseWayzimSmartCameraSdk
                    ? new WayzimSmartCamera().EnumerateCameras()
                    : emptyCameraTask;
                var wayzimIndustrialTask = selector.IsUseWayzimIndustrialCameraSdk
                    ? new WayzimIndustrialCamera().EnumerateCameras()
                    : emptyCameraTask;
                var hikvisionVolumeTask = selector.IsUseHikvisionVolumeCameraSdk
                    ? new HikvisionVolumeCamera().EnumerateCameras()
                    : emptyCameraTask;
                var daHuaVolumeTask = selector.IsUseDaHuaVolumeCameraSdk
                    ? new DaHuaVolumeCamera().EnumerateCameras()
                    : emptyCameraTask;
                var dimensionVolumeTask = selector.IsUseDimensionVolumeCameraSdk
                    ? new DimensionVolumeCamera().EnumerateCameras()
                    : emptyCameraTask;
                var normalUsbTask = selector.IsUsbCameraSdk
                    ? new NormalUsbCamera().EnumerateCameras()
                    : emptyCameraTask;

                await Task.WhenAll(
                        daHuaSmartTask,
                        hikvisionIndustrialTask,
                        hikvisionSmartTask,
                        daHuaSecurityTask,
                        wayzimSmartTask,
                        wayzimIndustrialTask,
                        hikvisionVolumeTask,
                        daHuaVolumeTask,
                        dimensionVolumeTask,
                        normalUsbTask)
                    .ConfigureAwait(false);

                var cameraGroups = new List<CameraInfo>?[]
                {
                    await daHuaVolumeTask.ConfigureAwait(false),
                    await daHuaSmartTask.ConfigureAwait(false),
                    await wayzimIndustrialTask.ConfigureAwait(false),
                    await wayzimSmartTask.ConfigureAwait(false),
                    (await daHuaSecurityTask.ConfigureAwait(false))
                        ?.Where(camera => camera.Type == CameraType.VideoCamera)
                        .ToList(),
                    await hikvisionIndustrialTask.ConfigureAwait(false),
                    await hikvisionSmartTask.ConfigureAwait(false),
                    await hikvisionVolumeTask.ConfigureAwait(false),
                    await dimensionVolumeTask.ConfigureAwait(false),
                    await normalUsbTask.ConfigureAwait(false)
                };

                _cameraInfos.Clear();
                foreach (var cameraGroup in cameraGroups)
                {
                    if (cameraGroup is null)
                    {
                        continue;
                    }

                    foreach (var camera in cameraGroup)
                    {
                        if (string.IsNullOrWhiteSpace(camera.SerialNumber))
                        {
                            continue;
                        }

                        _cameraInfos.AddOrUpdate(
                            camera.SerialNumber,
                            static (_, currentCamera) => currentCamera,
                            static (_, _, currentCamera) => currentCamera,
                            camera);
                    }
                }

                var list = _cameraInfos.Values.ToList();
                var itemInfoModels = list.Select(s => new CameraFinderItemInfoModel
                {
                    SerialNumber = s.SerialNumber,
                    Model = s.Model,
                    Name = s.Name,
                    IpAddress = s.IpAddress,
                    ConnectionType = s.ConnectionType,
                    //CameraType = ConvertCameraType(s.Brand, s.Model),
                    CameraType = s.Type,
                    Version = s.Version,
                    Brand = s.Brand,
                    IsOcrSupported = s.IsOcrSupported,
                    SupportedBindingType = s.SupportedBindingType
                }).ToList();
                CameraItems = list;
                CameraEnumerationRefreshed?.Invoke(this, itemInfoModels);
                return new KeyValuePair<bool, string>(true, Languages.Language.ResourceManager.GetString("相机检索成功") ?? string.Empty);
            }
            catch (Exception e)
            {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally
            {
                _cameraEnumerationGate.Release();
            }
        }

        public event EventHandler<CameraFinderItemInfoModel>? CameraBound;

        public DefaultDeviceService(IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IPanoramaCameraConfigRepository panoramaCameraConfigRepository,
            IVolumeCameraConfigRepository volumeCameraConfigRepository,
            ISettingsStore settingsStore, IDynamicScale dynamicScale,
            IStaticScale staticScale, IOcr ocr,
            IUsbCameraConfigRepository usbCameraConfigRepository,
            IKeyboardDeviceManager keyboardDeviceManager)
        {
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _panoramaCameraConfigRepository = panoramaCameraConfigRepository;
            _volumeCameraConfigRepository = volumeCameraConfigRepository;
            _settingsStore = settingsStore;
            _dynamicScale = dynamicScale;
            _staticScale = staticScale;
            _ocr = ocr;
            _usbCameraConfigRepository = usbCameraConfigRepository;
            _keyboardDeviceManager = keyboardDeviceManager;
            //注册磅秤事件
            _dynamicScale.StabledWeight += delegate (object? sender, float f)
            {
                OnStableWeight(new StableWeightEventArgs()
                {
                    Scale = (IScale?)sender,
                    Weight = f
                });
            };
            _dynamicScale.WeightStabilized += delegate (object? sender, WeightChangedEventArgs args)
            {
                OnWeightStabilized(args);
            };
            _dynamicScale.Connected += delegate (object? sender, IScale scale)
            {
                if (_weightSettingsDto is not null)
                {
                    OnScaleConnected(new ScaleConnectedEventArgs()
                    {
                        ConnectionParameters = new BaseScaleConnectParam()
                        {
                            Mode = _weightSettingsDto.ScaleCommunicationMode,
                            SerialPortInfo = new SerialPortConnectParam()
                            {
                                BaudRate = _weightSettingsDto.Connection.BaudRate,
                                DataFormat = (FormatType)_weightSettingsDto.Connection.DataFormat,
                                DataBits = _weightSettingsDto.Connection.DataBits,
                                Parity = _weightSettingsDto.Connection.Parity,
                                PortName = _weightSettingsDto.Connection.PortName,
                                StopBits = _weightSettingsDto.Connection.StopBits
                            },
                            TcpConnectInfo = new TcpConnectParam()
                            {
                                ClientConfig = new TcpParamInfo()
                                {
                                    IpAddress = _weightSettingsDto.TcpSettingsInfo.ClientConfig.IpAddress,
                                    Port = _weightSettingsDto.TcpSettingsInfo.ClientConfig.Port,
                                },
                                ServerConfig = new TcpParamInfo()
                                {
                                    IpAddress = _weightSettingsDto.TcpSettingsInfo.ServerConfig.IpAddress,
                                    Port = _weightSettingsDto.TcpSettingsInfo.ServerConfig.Port,
                                },
                                ConnectionMode = (TcpConnectionMode?)_weightSettingsDto.TcpSettingsInfo.ConnectionMode,
                                DataFormat = (FormatType)_weightSettingsDto.TcpSettingsInfo.DataFormat
                            }
                        },
                        ScaleType = ScaleType.Dynamic
                    });
                }
            };
            _dynamicScale.Excepted += delegate (object? sender, Exception exception)
            {
                OnDeviceException(new DeviceExceptionEventArgs()
                {
                    ExceptionMessage = exception
                });
            };
            _staticScale.StabledWeight += delegate (object? sender, float f)
            {
                OnStableWeight(new StableWeightEventArgs()
                {
                    Scale = (IScale?)sender,
                    Weight = f
                });
            };
            _staticScale.Excepted += delegate (object? sender, Exception exception)
            {
                //异常的输出之后需要取消
                OnDeviceException(new DeviceExceptionEventArgs()
                {
                    ExceptionMessage = exception
                });
            };
            _staticScale.WeightStabilized += delegate (object? sender, WeightChangedEventArgs args)
            {
                OnWeightStabilized(args);
            };
            _staticScale.WeightCleared += (sender, args) =>
            {
                OnWeightCleared(args);
            };
            _staticScale.Connected += delegate (object? sender, IScale scale)
            {
                if (_weightSettingsDto is not null)
                {
                    OnScaleConnected(new ScaleConnectedEventArgs()
                    {
                        ConnectionParameters = new BaseScaleConnectParam()
                        {
                            Mode = _weightSettingsDto.ScaleCommunicationMode,
                            SerialPortInfo = new SerialPortConnectParam()
                            {
                                BaudRate = _weightSettingsDto.Connection.BaudRate,
                                DataFormat = (FormatType)_weightSettingsDto.Connection.DataFormat,
                                DataBits = _weightSettingsDto.Connection.DataBits,
                                Parity = _weightSettingsDto.Connection.Parity,
                                PortName = _weightSettingsDto.Connection.PortName,
                                StopBits = _weightSettingsDto.Connection.StopBits
                            },
                            TcpConnectInfo = new TcpConnectParam()
                            {
                                ClientConfig = new TcpParamInfo()
                                {
                                    IpAddress = _weightSettingsDto.TcpSettingsInfo.ClientConfig.IpAddress,
                                    Port = _weightSettingsDto.TcpSettingsInfo.ClientConfig.Port,
                                },
                                ServerConfig = new TcpParamInfo()
                                {
                                    IpAddress = _weightSettingsDto.TcpSettingsInfo.ServerConfig.IpAddress,
                                    Port = _weightSettingsDto.TcpSettingsInfo.ServerConfig.Port,
                                },
                                ConnectionMode = (TcpConnectionMode?)_weightSettingsDto.TcpSettingsInfo.ConnectionMode,
                                DataFormat = (FormatType)_weightSettingsDto.TcpSettingsInfo.DataFormat
                            }
                        },
                        ScaleType = ScaleType.Static
                    });
                }
            };
            //扫码枪
            _keyboardDeviceManager.BarCodeReceived += (sender, s) =>
            {
                OnBarCodeKeyReceived(s);
            };
            _keyboardDeviceManager.RealTimeKeyReceived += (sender, s) =>
            {
                OnRealTimeKeyReceived(s);
            };
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async settings =>
            {
                if (settings is SettingsChangedEvent { SettingsName: "BarcodeFilterSettings" })
                {
                    _barcodeFilterSettingsDto = await _settingsStore.GetAsync<BarcodeFilterSettingsDto>("BarcodeFilterSettings") ??
                        new BarcodeFilterSettingsDto();

                    if (RunningStatus)
                    {
                        OnDeviceException(new DeviceExceptionEventArgs()
                        {
                            ExceptionMessage = new Exception($"{Languages.Language.ResourceManager.GetString("必须先停止运行再设置条码过滤才能生效") ?? string.Empty}")
                        });
                    }
                }
                else if (settings is SettingsChangedEvent { SettingsName: "SaveImageSettings" })
                {
                    try
                    {
                        var imageSettings = await _settingsStore
                            .GetAsync<ImageSettingsDto>("SaveImageSettings")
                            ?? new ImageSettingsDto();
                        Volatile.Write(ref _imageSettingsDto, imageSettings);
                        ApplyImageOutputSettings(Volatile.Read(ref _cameras));
                    }
                    catch (Exception exception)
                    {
                        OnDeviceException(new DeviceExceptionEventArgs()
                        {
                            ExceptionMessage = new Exception($"加载存图设置失败:{exception.Message}")
                        });
                    }
                }
                else if (settings is SettingsChangedEvent { SettingsName: "CameraSdkSelector" })
                {
                    try
                    {
                        _cameraSdkSelectorDto = await _settingsStore
                            .GetAsync<CameraSdkSelectorDto>("CameraSdkSelector") ??
                            new CameraSdkSelectorDto();
                    }
                    catch (Exception e)
                    {
                        OnDeviceException(new DeviceExceptionEventArgs()
                        {
                            ExceptionMessage = new Exception($"{e.Message}")
                        });
                    }
                }
            });
        }

        public async Task<KeyValuePair<bool, string>> OnCameraBound(CameraFinderItemInfoModel camera, CancellationToken token = default)
        {
            await _deviceLifecycleGate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (RunningStatus)
                {
                    return new KeyValuePair<bool, string>(false, $"{Languages.Language.ResourceManager.GetString("设备运行中则不能解绑或者绑定") ?? string.Empty}");
                }

                CameraBound?.Invoke(this, camera);
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
            finally
            {
                _deviceLifecycleGate.Release();
            }
        }

        public event EventHandler<CameraFinderItemInfoModel>? CameraUnbound;

        public async Task<KeyValuePair<bool, string>> OnCameraUnbound(CameraFinderItemInfoModel camera, CancellationToken token = default)
        {
            await _deviceLifecycleGate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (RunningStatus)
                {
                    return new KeyValuePair<bool, string>(false, $"{Languages.Language.ResourceManager.GetString("设备运行中则不能解绑或者绑定") ?? string.Empty}");
                }

                CameraUnbound?.Invoke(this, camera);
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
            finally
            {
                _deviceLifecycleGate.Release();
            }
        }

        public event EventHandler<List<CameraParametersModifiedEventArgs>>? CameraParametersModified;

        public async Task<KeyValuePair<bool, string>> OnCameraParametersModified(List<CameraParametersModifiedEventArgs> camera, CancellationToken token = default)
        {
            await _deviceLifecycleGate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (RunningStatus)
                {
                    return new KeyValuePair<bool, string>(false, $"{Languages.Language.ResourceManager.GetString("设备运行中则不能解绑或者绑定") ?? string.Empty}");
                }

                var scannerCameraConfigsTask =
                    _barcodeScannerCameraConfigRepository.Select(static item => item.Id > 0,
                        static item => item.Id, token);
                var panoramaCameraConfigsTask =
                    _panoramaCameraConfigRepository.Select(static item => item.Id > 0,
                        static item => item.Id, token);
                var volumeCameraConfigsTask =
                    _volumeCameraConfigRepository.Select(static item => item.Id > 0,
                        static item => item.Id, token);
                await Task.WhenAll(
                        scannerCameraConfigsTask,
                        panoramaCameraConfigsTask,
                        volumeCameraConfigsTask)
                    .ConfigureAwait(false);
                var scannerCameraConfigInfoModels =
                    await scannerCameraConfigsTask.ConfigureAwait(false);
                var panoramaCameraConfigInfoModels =
                    await panoramaCameraConfigsTask.ConfigureAwait(false);
                var volumeCameraConfigInfoModels =
                    await volumeCameraConfigsTask.ConfigureAwait(false);
                var releasedCameras = Interlocked.Exchange(ref _cameras, []);
                DisposeCameraCollection(releasedCameras);
                //保存绑定参数
                _cameraParameters.Clear();
                _cameraParameters.EnsureCapacity(
                    scannerCameraConfigInfoModels.Count +
                    panoramaCameraConfigInfoModels.Count +
                    volumeCameraConfigInfoModels.Count);
                _cameraParameters.AddRange(scannerCameraConfigInfoModels.Select(s => new CameraParametersModifiedEventArgs
                {
                    Type = CameraBindingType.ScannerCamera,
                    Parameters = s
                }));
                _cameraParameters.AddRange(panoramaCameraConfigInfoModels.Select(s => new CameraParametersModifiedEventArgs
                {
                    Type = CameraBindingType.PanoramaCamera,
                    Parameters = s
                }));
                _cameraParameters.AddRange(volumeCameraConfigInfoModels.Select(s => new CameraParametersModifiedEventArgs
                {
                    Type = CameraBindingType.VolumeCamera,
                    Parameters = s
                }));

                CameraParametersModified?.Invoke(this, camera);
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
            finally
            {
                _deviceLifecycleGate.Release();
            }
        }

        public event EventHandler<string>? CameraReleased;

        public event EventHandler<CameraStartedEventArgs>? CameraStarted;

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

        public async Task<KeyValuePair<bool, string>> Start(CancellationToken token = default)
        {
            await _deviceLifecycleGate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                return await StartCore(token).ConfigureAwait(false);
            }
            finally
            {
                _deviceLifecycleGate.Release();
            }
        }

        private async Task<KeyValuePair<bool, string>> StartCore(CancellationToken token)
        {
            if (RunningStatus)
            {
                return new KeyValuePair<bool, string>(true, "设备服务已启动");
            }

            var startupErrors = new List<string>();
            //在这里初始化
            try
            {
                await InitializationCore().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Interlocked.Exchange(ref _runningStatus, 0);
                return new KeyValuePair<bool, string>(false, $"设备初始化失败:{exception.Message}");
            }
            //启动(逐个相机启动)
            var cameras = Volatile.Read(ref _cameras);
            foreach (var camera in cameras.OrderByDescending(o => o.BindingType))
            {
                //设置过滤
                if (camera.BindingType is CameraBindingType.ScannerCamera or CameraBindingType.OcrCamera)
                {
                    var filterParams = new ScanCodeFilterParams
                    {
                        DuplicateBarcodeFilterCount = _barcodeFilterSettingsDto?.DuplicateBarcodeFilterCount ?? 0,
                        RegularExpression = _barcodeFilterSettingsDto?.BasicFilterInfo?.RegularExpression ?? string.Empty,
                        ScanInterval = _barcodeFilterSettingsDto?.ScanInterval ?? 1000,
                        FilterOutContent = _barcodeFilterSettingsDto?.FilterOutputType switch
                        {
                            FilterOutputType.NoRead => "NoRead",
                            FilterOutputType.Filtered => "Filtered",
                            _ => string.Empty
                        },
                        BarCodeFilterMode = (BarCodeFilterMode)(_barcodeFilterSettingsDto?.BarCodeFilterOptions ?? BarCodeFilterOptions.None),
                        IsUseCustomRegexReplacement = _barcodeFilterSettingsDto?.IsUseCustomRegexReplacement ?? false,
                        CustomRegexReplacementItems = _barcodeFilterSettingsDto?.CustomRegexReplacementItems?.Where(w => w.IsActive)?.Select(s =>
                            new CustomRegexReplacementItemInfo
                            {
                                RegexPattern = s.RegexPattern,
                                ReplaceContent = s.ReplaceContent
                            })?.ToList() ?? new List<CustomRegexReplacementItemInfo>(),
                        CustomRegularExpressionItems = _barcodeFilterSettingsDto?.CustomRegexFilterItems?.Where(w => w.IsActive)?
                            .Select(s => s.RegexPattern)?.ToList() ?? new List<string>()
                    };
                    switch (camera)
                    {
                        case IIndustrialCamera industrialCamera:
                            industrialCamera.SetScanCodeFilterParams(filterParams);
                            break;

                        case ISmartCamera smartCamera:
                            smartCamera.SetScanCodeFilterParams(filterParams);
                            break;

                        case ISecurityCamera securityCamera:
                            securityCamera.SetScanCodeFilterParams(filterParams);
                            break;
                    }
                }

                await Task.Delay(100, token);
                var (cameraStarted, cameraStartMessage) = await camera.Start(string.Empty);
                if (!cameraStarted)
                {
                    startupErrors.Add(
                        $"相机[{camera.Info?.CustomName ?? camera.Info?.SerialNumber ?? camera.SdkName}]启动失败:{cameraStartMessage}");
                    continue;
                }

                if (_cameraSdkSelectorDto?.IsUsbCameraSdk == true && camera is NormalUsbCamera usbCamera)
                {
                    var usbCameraParameter = await GetUsbCameraParameter(usbCamera.Info?.SerialNumber ?? string.Empty) ?? new Dictionary<UsbCameraParameter, object>();
                    var barcodeReaderParameter = await GetBarcodeReaderParameter() ?? new Dictionary<BarcodeReaderParameter, object>();
                    var dictionary = new Dictionary<string, object>()
                    {
                        {"UsbCameraParameter", usbCameraParameter},
                        {"BarcodeReaderParameter", barcodeReaderParameter}
                    };
                    usbCamera.SetParameters(dictionary);
                }
                else if (camera is DaHuatechSecurityCamera ipcCamera && camera.BindingType == CameraBindingType.ScannerCamera)
                {
                    var barcodeReaderParameter = await GetBarcodeReaderParameter() ?? new Dictionary<BarcodeReaderParameter, object>();
                    var dictionary = new Dictionary<string, object>()
                    {
                        {"BarcodeReaderParameter", barcodeReaderParameter}
                    };
                    ipcCamera.SetParameters(dictionary);
                }
            }
            //连接磅秤
            if (_weightSettingsDto is not null)
            {
                switch (_weightSettingsDto.Mode)
                {
                    case WeightMode.Static:
                        var staticScaleConnected = await _staticScale.Connect(new BaseScaleConnectParam()
                        {
                            Mode = _weightSettingsDto.ScaleCommunicationMode,
                            SerialPortInfo = new SerialPortConnectParam()
                            {
                                BaudRate = _weightSettingsDto.Connection.BaudRate,
                                DataFormat = (FormatType)_weightSettingsDto.Connection.DataFormat,
                                DataBits = _weightSettingsDto.Connection.DataBits,
                                Parity = _weightSettingsDto.Connection.Parity,
                                PortName = _weightSettingsDto.Connection.PortName,
                                StopBits = _weightSettingsDto.Connection.StopBits
                            },
                            TcpConnectInfo = new TcpConnectParam()
                            {
                                ClientConfig = new TcpParamInfo()
                                {
                                    IpAddress = _weightSettingsDto.TcpSettingsInfo.ClientConfig.IpAddress,
                                    Port = _weightSettingsDto.TcpSettingsInfo.ClientConfig.Port,
                                },
                                ServerConfig = new TcpParamInfo()
                                {
                                    IpAddress = _weightSettingsDto.TcpSettingsInfo.ServerConfig.IpAddress,
                                    Port = _weightSettingsDto.TcpSettingsInfo.ServerConfig.Port,
                                },
                                ConnectionMode = (TcpConnectionMode?)_weightSettingsDto.TcpSettingsInfo.ConnectionMode,
                                DataFormat = (FormatType)_weightSettingsDto.TcpSettingsInfo.DataFormat
                            }
                        });
                        if (!staticScaleConnected)
                        {
                            startupErrors.Add("静态秤连接失败");
                        }
                        //连接静态称
                        break;

                    case WeightMode.Dynamic:
                        var dynamicScaleConnected = await _dynamicScale.Connect(new BaseScaleConnectParam()
                        {
                            Mode = _weightSettingsDto.ScaleCommunicationMode,
                            SerialPortInfo = new SerialPortConnectParam()
                            {
                                BaudRate = _weightSettingsDto.Connection.BaudRate,
                                DataFormat = (FormatType)_weightSettingsDto.Connection.DataFormat,
                                DataBits = _weightSettingsDto.Connection.DataBits,
                                Parity = _weightSettingsDto.Connection.Parity,
                                PortName = _weightSettingsDto.Connection.PortName,
                                StopBits = _weightSettingsDto.Connection.StopBits
                            },
                            TcpConnectInfo = new TcpConnectParam()
                            {
                                ClientConfig = new TcpParamInfo()
                                {
                                    IpAddress = _weightSettingsDto.TcpSettingsInfo.ClientConfig.IpAddress,
                                    Port = _weightSettingsDto.TcpSettingsInfo.ClientConfig.Port,
                                },
                                ServerConfig = new TcpParamInfo()
                                {
                                    IpAddress = _weightSettingsDto.TcpSettingsInfo.ServerConfig.IpAddress,
                                    Port = _weightSettingsDto.TcpSettingsInfo.ServerConfig.Port,
                                },
                                ConnectionMode = (TcpConnectionMode?)_weightSettingsDto.TcpSettingsInfo.ConnectionMode,
                                DataFormat = (FormatType)_weightSettingsDto.TcpSettingsInfo.DataFormat
                            }
                        });
                        if (!dynamicScaleConnected)
                        {
                            startupErrors.Add("动态秤连接失败");
                        }
                        break;
                }
            }
            //初始化扫码枪
            //获取已绑定的扫码枪
            var contentInputSettingsDto = await _settingsStore.GetAsync<ContentInputSettingsDto>("ContentInputSettings", token) ?? new ContentInputSettingsDto();
            if (contentInputSettingsDto.KeyboardDevice is { ProductId: > 0, VendorId: > 0 })
            {
                //设置过滤
                if (contentInputSettingsDto.IsUseRegularFilter)
                {
                    _keyboardDeviceManager.SetFilterRule(_barcodeFilterSettingsDto?.BasicFilterInfo?.RegularExpression ?? string.Empty);
                }
                var listening = await _keyboardDeviceManager.StartListening(contentInputSettingsDto.KeyboardDevice);
                if (!listening)
                {
                    startupErrors.Add("扫码枪监听失败");
                    OnDeviceException(new DeviceExceptionEventArgs()
                    {
                        ExceptionMessage = new Exception("扫码枪监听失败")
                    });
                }
            }

            if (startupErrors.Count > 0)
            {
                DisposeCore();
                Interlocked.Exchange(ref _runningStatus, 0);
                return new KeyValuePair<bool, string>(false, string.Join(Environment.NewLine, startupErrors));
            }

            Interlocked.Exchange(ref _runningStatus, 1);
            return new KeyValuePair<bool, string>(true, "设备服务启动成功");
        }

        public async Task<KeyValuePair<bool, string>> Stop(CancellationToken token = default)
        {
            await _deviceLifecycleGate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                DisposeCore();
                Interlocked.Exchange(ref _runningStatus, 0);
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
            finally
            {
                _deviceLifecycleGate.Release();
            }
        }

        public event EventHandler<KeyboardBarCodeReceivedEventArgs>? BarCodeKeyReceived;

        public event EventHandler<KeyboardRealTimeKeyEventArgs>? RealTimeKeyReceived;

        public async Task Initialization()
        {
            await _deviceLifecycleGate.WaitAsync().ConfigureAwait(false);
            try
            {
                await InitializationCore().ConfigureAwait(false);
            }
            finally
            {
                _deviceLifecycleGate.Release();
            }
        }

        private async Task InitializationCore()
        {
            if (RunningStatus)
            {
                return;
            }

            // 相机原生SDK初始化包含同步阻塞调用，统一放在线程池，避免阻塞调用方线程。
            await Task.Run(async () =>
            {
                OcrSettingsDto ocrSettingsDto = new();
                var initializedCameras = new List<ICamera>();
                var camerasPublished = false;
                try
                {
                    //如果没有枚举过相机就需要在这里枚举
                    if (_cameraInfos.Count == 0)
                    {
                        var enumerationResult = await OnCameraEnumerationRefreshed();
                        if (!enumerationResult.Key)
                        {
                            throw new InvalidOperationException(enumerationResult.Value);
                        }
                    }

                    var barcodeFilterSettingsTask =
                        _settingsStore.GetAsync<BarcodeFilterSettingsDto>(
                            "BarcodeFilterSettings");
                    var ocrSettingsTask =
                        _settingsStore.GetAsync<OcrSettingsDto>("OcrSettings");
                    var scannerCameraConfigsTask =
                        _barcodeScannerCameraConfigRepository.Select(static camera => camera.Id > 0,
                            static camera => camera.Id);
                    var panoramaCameraConfigsTask =
                        _panoramaCameraConfigRepository.Select(static camera => camera.Id > 0,
                            static camera => camera.Id);
                    var volumeCameraConfigsTask =
                        _volumeCameraConfigRepository.Select(static camera => camera.Id > 0,
                            static camera => camera.Id);
                    var weightSettingsTask =
                        _settingsStore.GetAsync<WeightSettingsDto>("WeightSettings");
                    var createPackageSettingsTask =
                        _settingsStore.GetAsync<CreatePackageSettingsDto>(
                            "CreatePackageSettings");
                    var imageSettingsTask =
                        _settingsStore.GetAsync<ImageSettingsDto>(
                            "SaveImageSettings");
                    await Task.WhenAll(
                        barcodeFilterSettingsTask,
                        ocrSettingsTask,
                        scannerCameraConfigsTask,
                        panoramaCameraConfigsTask,
                        volumeCameraConfigsTask,
                        weightSettingsTask,
                        createPackageSettingsTask,
                        imageSettingsTask);

                    _barcodeFilterSettingsDto =
                        await barcodeFilterSettingsTask ?? new BarcodeFilterSettingsDto();
                    ocrSettingsDto = await ocrSettingsTask ?? new OcrSettingsDto();
                    var scannerCameraConfigInfoModels = await scannerCameraConfigsTask;
                    var panoramaCameraConfigInfoModels = await panoramaCameraConfigsTask;
                    var volumeCameraConfigInfoModels = await volumeCameraConfigsTask;
                    _weightSettingsDto = await weightSettingsTask ?? new WeightSettingsDto();
                    var createPackageSettingsDto =
                        await createPackageSettingsTask ?? new CreatePackageSettingsDto();
                    Volatile.Write(
                        ref _imageSettingsDto,
                        await imageSettingsTask ?? new ImageSettingsDto());

                    try
                    {
                        var modelFilePath = ocrSettingsDto.ModelFilePath;
                        var modelDirectory =
                            Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "OnnxModels");
                        Directory.CreateDirectory(modelDirectory);
                        if (string.IsNullOrEmpty(modelFilePath))
                        {
                            modelFilePath = Directory
                                .EnumerateFiles(modelDirectory, "*.onnx", SearchOption.TopDirectoryOnly)
                                .FirstOrDefault() ?? string.Empty;
                        }
                        else
                        {
                            var configuredModelPath =
                                Path.Combine(modelDirectory, Path.GetFileName(modelFilePath));
                            modelFilePath = File.Exists(configuredModelPath)
                                ? configuredModelPath
                                : string.Empty;
                        }
                        await _ocr.SetOnnxModelPath(modelFilePath);
                        await _ocr.SetConfidenceThreshold(ocrSettingsDto.ConfidenceThreshold);
                        await _ocr.SetRectangleScale(ocrSettingsDto.RectangleScale);
                        await _ocr.SetIsSecondConfirmationEnabled(ocrSettingsDto.IsSecondConfirmationEnabled);
                    }
                    catch (Exception e)
                    {
                        OnDeviceException(new DeviceExceptionEventArgs()
                        {
                            ExceptionMessage = new Exception($"加载Ocr设置识别")
                        });
                    }
                    var scannerConfigsBySerial =
                        CreateCameraConfigLookup(scannerCameraConfigInfoModels);
                    var panoramaConfigsBySerial =
                        CreateCameraConfigLookup(panoramaCameraConfigInfoModels);

                    _cameraParameters.Clear();
                    _cameraParameters.EnsureCapacity(
                        scannerCameraConfigInfoModels.Count +
                        panoramaCameraConfigInfoModels.Count +
                        volumeCameraConfigInfoModels.Count);
                    _cameraParameters.AddRange(scannerCameraConfigInfoModels.Select(s => new CameraParametersModifiedEventArgs
                    {
                        Type = CameraBindingType.ScannerCamera,
                        Parameters = s
                    }));
                    _cameraParameters.AddRange(panoramaCameraConfigInfoModels.Select(s => new CameraParametersModifiedEventArgs
                    {
                        Type = CameraBindingType.PanoramaCamera,
                        Parameters = s
                    }));
                    _cameraParameters.AddRange(volumeCameraConfigInfoModels.Select(s => new CameraParametersModifiedEventArgs
                    {
                        Type = CameraBindingType.VolumeCamera,
                        Parameters = s
                    }));

                    //初始化已经绑定的相机

                    foreach (var parameter in _cameraParameters)
                    {
                        ICamera? camera = null;

                        //创建对象
                        switch (parameter.Type)
                        {
                            case CameraBindingType.ScannerCamera:
                                {
                                    //扫码相机
                                    if (parameter.Parameters is BarcodeScannerCameraConfigInfoModel model)
                                    {
                                        var tryGetValue = _cameraInfos.TryGetValue(model.SerialNumber, out var info);
                                        if (tryGetValue && info is not null)
                                        {
                                            //转换绑定
                                            camera = ConvertCamera(info);
                                            if (camera is not null)
                                            {
                                                //设置绑定模式
                                                //判断是否使用Ocr
                                                camera.BindingType = model.IsOcrSupported ? CameraBindingType.OcrCamera : CameraBindingType.ScannerCamera;
                                                if (camera.BindingType == CameraBindingType.OcrCamera &&
                                                    !ocrSettingsDto.IsUseOcr)
                                                {
                                                    camera = null;
                                                }
                                            }
                                        }
                                    }

                                    break;
                                }
                            case CameraBindingType.PanoramaCamera:
                                {
                                    //全景相机
                                    if (parameter.Parameters is PanoramaCameraConfigInfoModel model)
                                    {
                                        var tryGetValue = _cameraInfos.TryGetValue(model.SerialNumber, out var info);
                                        if (tryGetValue && info is not null)
                                        {
                                            //转换绑定
                                            camera = ConvertCamera(info);
                                            if (camera?.Info is not null)
                                            {
                                                //设置绑定模式
                                                camera.BindingType = CameraBindingType.PanoramaCamera;
                                                camera.Info.Type = (CameraType)model.CameraType;
                                            }
                                            else
                                            {
                                                camera = null;
                                            }
                                        }
                                    }

                                    break;
                                }
                            case CameraBindingType.VolumeCamera:
                                {
                                    //体积相机
                                    if (parameter.Parameters is VolumeCameraConfigInfoModel model)
                                    {
                                        var tryGetValue = _cameraInfos.TryGetValue(model.SerialNumber, out var info);
                                        if (tryGetValue && info is not null)
                                        {
                                            //转换绑定
                                            camera = ConvertCamera(info);
                                            if (camera is not null)
                                            {
                                                //设置绑定模式
                                                camera.BindingType = CameraBindingType.VolumeCamera;
                                            }
                                        }
                                    }

                                    break;
                                }
                        }

                        if (camera is not null)
                        {
                            ApplyImageOutputSettings([camera]);
                            var cameraInfo = camera.Info;
                            if (cameraInfo is null)
                            {
                                camera.Dispose();
                                continue;
                            }

                            //注册事件
                            scannerConfigsBySerial.TryGetValue(
                                cameraInfo.SerialNumber, out var scannerCameraConfig);
                            panoramaConfigsBySerial.TryGetValue(
                                cameraInfo.SerialNumber, out var panoramaCameraConfig);
                            var isShowRealTimeImage =
                                scannerCameraConfig?.IsShowRealTimeImage ?? false;
                            camera.CameraDisconnected += delegate (object? sender, CameraConnectionEventArgs args)
                            {
                                if (sender is ICamera mCamera)
                                {
                                    OnCameraDisconnected(mCamera);
                                }
                            };
                            camera.CameraExceptionOccurred += delegate (object? sender, CameraExceptionEventArgs args)
                            {
                                var mCameraInfo = string.Empty;
                                if (sender is ICamera mCamera)
                                {
                                    mCameraInfo =
                                        $"ID:{mCamera.Info?.Id},SerialNumber:{mCamera?.Info?.SerialNumber},SdkType:{mCamera?.SdkType}";
                                }
                                OnCameraException(new DeviceExceptionEventArgs()
                                {
                                    ExceptionMessage = new Exception($"{args.Exception?.Message}")
                                });
                                OnDeviceException(new DeviceExceptionEventArgs()
                                {
                                    ExceptionMessage = new Exception($"{mCameraInfo}-{args.Exception?.Message}")
                                });
                            };
                            camera.PhotoTaken += delegate (object? sender, PhotoTakenEventArgs args)
                            {
                                OnPanoramaCaptured(new PanoramaCaptureEventArgs()
                                {
                                    CameraSerialNumber = args.CameraSerialNumber,
                                    Image = args.Image,
                                    PhotoTime = args.PhotoTime,
                                    Timestamp = args.Timestamp,
                                    ThumbImage = args.ThumbImage,
                                    Barcode = args.Barcode,
                                    BarcodeTimestamp = args.BarcodeTimestamp
                                });
                            };
                            camera.RealtimeImage += delegate (object? sender, RealtimeImageEventArgs args)
                            {
                                OnRealTimeImage(new RealTimeImageEventArgs()
                                {
                                    Camera = camera,
                                    Image = args.ThumbImage,
                                });
                            };
                            //相机启动事件
                            camera.CameraStarted += (sender, args) =>
                            {
                                OnCameraStarted(args);
                            };
                            //判断相机类型(各自注册事件)

                            switch (camera)
                            {
                                case IIndustrialCamera industrialCamera:
                                    industrialCamera.TakePhotoDelay =
                                        panoramaCameraConfig?.CaptureDelayTime ?? 0;
                                    //填充其他信息
                                    industrialCamera.BarcodeRead += delegate (object? sender, BarcodeReadEventArgs args)
                                    {
                                        OnBarcodeScanned(args);
                                    };
                                    industrialCamera.OcrContentRecognized += delegate (object? sender,
                                        OcrResult args)
                                    {
                                        OnOcrContentRecognized(args);
                                    };
                                    industrialCamera.CameraStarted += (sender, args) =>
                                    {
                                        if (isShowRealTimeImage == true)
                                        {
                                            industrialCamera.StartRealTimeImage();
                                        }
                                    };

                                    if (industrialCamera.BindingType == CameraBindingType.OcrCamera)
                                    {
                                        industrialCamera.Ocr = _ocr;
                                    }
                                    break;

                                case ISmartCamera smartCamera:
                                    smartCamera.BarcodeReadTriggered +=
                                        delegate (object? sender, BarcodeTriggeredEventArgs args)
                                        {
                                            OnBarcodeScanned(args);
                                        };
                                    smartCamera.NotBarcodeHitEvent += delegate (object? sender, BarcodeReadEventArgs args)
                                    {
                                        OnNotBarcodeHitEvent(args);
                                    };
                                    smartCamera.OcrContentRecognized += delegate (object? sender,
                                        OcrResult args)
                                    {
                                        OnOcrContentRecognized(args);
                                    };
                                    try
                                    {
                                        var parameters =
                                            scannerCameraConfig?.CameraConnectionParameters;
                                        if (!string.IsNullOrEmpty(parameters))
                                        {
                                            var jObject = JObject.Parse(parameters);
                                            if (jObject["TriggerMode"] is not null)
                                            {
                                                smartCamera.TriggerMode = (TriggerMode)(jObject["TriggerMode"] ?? 0).Value<int>();
                                            }

                                            if (jObject["SourceLine"] is not null)
                                            {
                                                smartCamera.SourceLine = (jObject["SourceLine"] ?? 0).Value<int>();
                                            }
                                        }

                                        smartCamera.CameraStarted += (sender, args) =>
                                        {
                                            if (isShowRealTimeImage == true)
                                            {
                                                smartCamera.StartRealTimeImage();
                                            }
                                        };

                                        if (smartCamera.BindingType == CameraBindingType.OcrCamera)
                                        {
                                            smartCamera.Ocr = _ocr;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                                    }

                                    break;

                                case ISecurityCamera securityCamera:
                                    {
                                        var parameters = securityCamera.BindingType switch
                                        {
                                            CameraBindingType.PanoramaCamera =>
                                                panoramaCameraConfig?.CameraConnectionParameters,
                                            CameraBindingType.ScannerCamera or
                                                CameraBindingType.OcrCamera =>
                                                scannerCameraConfig?.CameraConnectionParameters,
                                            _ => string.Empty
                                        };

                                        securityCamera.CameraConnectionParameters =
                                                parameters ?? string.Empty;

                                        securityCamera.CameraStarted += (sender, args) =>
                                        {
                                            if (isShowRealTimeImage == true)
                                            {
                                                securityCamera.StartRealTimeImage();
                                            }
                                        };
                                        securityCamera.BarcodeRead += (sender, args) =>
                                        {
                                            OnBarcodeScanned(args);
                                        };

                                        securityCamera.OcrContentRecognized += delegate (object? sender,
                                            OcrResult args)
                                        {
                                            OnOcrContentRecognized(args);
                                        };
                                        break;
                                    }
                                case IVolumeCamera volumeCamera:
                                    {
                                        volumeCamera.VolumeCaptured += delegate (object? sender,
                                            VolumeCapturedEventArgs args)
                                        {
                                            OnVolumeCaptured(args);
                                        };
                                        break;
                                    }
                            }

                            //初始化
                            var (b, s) = await camera.Initialize(cameraInfo);
                            if (!b)
                            {
                                OnDeviceException(new DeviceExceptionEventArgs()
                                {
                                    ExceptionMessage = new Exception(s)
                                });
                                camera.Dispose();
                                continue;
                            }

                            //添加到集合
                            initializedCameras.Add(camera);
                        }
                    }
                    var cameraSnapshot = initializedCameras.ToArray();
                    Volatile.Write(ref _cameras, cameraSnapshot);
                    camerasPublished = true;
                    OnCameraInitialized([.. cameraSnapshot]);
                    //磅秤相关
                    try
                    {
                        if (_weightSettingsDto.Mode != WeightMode.None)
                        {
                            _staticScale.Dispose();
                            _dynamicScale.Dispose();
                            await Task.Delay(TimeSpan.FromSeconds(1));
                            //判断需要连接的磅秤
                            var properties = new WeightAdditionalProperties()
                            {
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
                            switch (_weightSettingsDto.Mode)
                            {
                                //连接
                                case WeightMode.Static:
                                    ScaleType = ScaleType.Static;
                                    _staticScale.WeightFormat = (ScaleWeightFormat)_weightSettingsDto.Connection.DataFormat;
                                    _staticScale.WeightAdditionalProperties = properties;
                                    _staticScale.SetWeightCalculationParameters(new DefaultStaticScaleValueParameters()
                                    {
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
                                    _dynamicScale.SetWeightCalculationParameters(new DefaultDynamicScaleValueParameters()
                                    {
                                        DecimalPlaces = _weightSettingsDto.DynamicWeight.DecimalPrecision
                                    });

                                    break;

                                case WeightMode.None:
                                    ScaleType = ScaleType.None;
                                    break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        OnDeviceException(new DeviceExceptionEventArgs()
                        {
                            ExceptionMessage = new Exception($"{Languages.Language.ResourceManager.GetString("加载磅秤设置失败") ?? string.Empty}:{e.Message}")
                        });
                    }
                    //扫码枪相关

                    if (createPackageSettingsDto.PackageCreationMethods.HasFlag(PackageCreationMethodsEnum.BarcodeScannerInput))
                    {
                        await _keyboardDeviceManager.EnumerateKeyboardDevices();
                    }
                }
                catch (Exception e)
                {
                    if (camerasPublished)
                    {
                        DisposeCore();
                    }
                    else
                    {
                        DisposeCameraCollection(initializedCameras);
                    }

                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                    throw;
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 按相机序列号创建配置索引，重复配置保留排序后的首项。
        /// </summary>
        private static Dictionary<string, TConfig> CreateCameraConfigLookup<TConfig>(
            IEnumerable<TConfig> cameraConfigs)
            where TConfig : BaseCameraConfigInfoModel
        {
            var configsBySerial =
                new Dictionary<string, TConfig>(StringComparer.OrdinalIgnoreCase);
            foreach (var cameraConfig in cameraConfigs)
            {
                if (!string.IsNullOrWhiteSpace(cameraConfig.SerialNumber))
                {
                    configsBySerial.TryAdd(cameraConfig.SerialNumber, cameraConfig);
                }
            }

            return configsBySerial;
        }

        /// <summary>
        /// 释放尚未发布的相机集合，初始化失败时避免原生句柄泄漏。
        /// </summary>
        private void DisposeCameraCollection(IReadOnlyList<ICamera> cameras)
        {
            for (var index = cameras.Count - 1; index >= 0; index--)
            {
                try
                {
                    cameras[index].Dispose();
                }
                catch (Exception exception)
                {
                    ReportDisposeException("释放初始化失败相机异常", exception);
                }
            }
        }

        public void Dispose()
        {
            _deviceLifecycleGate.Wait();
            try
            {
                DisposeCore();
                Interlocked.Exchange(ref _runningStatus, 0);
            }
            finally
            {
                _deviceLifecycleGate.Release();
            }
        }

        private void DisposeCore()
        {
            var cameras = Interlocked.Exchange(ref _cameras, []);
            for (var i = cameras.Length - 1; i >= 0; i--)
            {
                var camera = cameras[i];
                var serialNumber = camera?.Info?.SerialNumber ?? string.Empty;
                try
                {
                    camera?.Dispose();
                }
                catch (Exception exception)
                {
                    ReportDisposeException("释放相机异常", exception);
                }
                finally
                {
                    OnCameraReleased(serialNumber);
                }
            }

            try
            {
                _dynamicScale?.Dispose();
            }
            catch (Exception exception)
            {
                ReportDisposeException("释放动态秤异常", exception);
            }

            try
            {
                _staticScale?.Dispose();
            }
            catch (Exception exception)
            {
                ReportDisposeException("释放静态秤异常", exception);
            }

            try
            {
                _keyboardDeviceManager.Dispose();
            }
            catch (Exception exception)
            {
                ReportDisposeException("释放扫码枪异常", exception);
            }
        }

        /// <summary>
        /// 上报资源释放异常，同时继续释放其他设备。
        /// </summary>
        private void ReportDisposeException(string operation, Exception exception)
        {
            OnDeviceException(new DeviceExceptionEventArgs
            {
                ExceptionMessage = new Exception($"{operation}:{exception.Message}", exception)
            });
            NLog.LogManager.GetCurrentClassLogger().Error(exception, operation);
        }

        protected virtual void OnCameraInitialized(List<ICamera> e)
        {
            CameraInitialized?.Invoke(this, e);
        }

        protected virtual void OnCameraDisconnected(ICamera e)
        {
            ICamera[] current;
            ICamera[] updated;
            do
            {
                current = Volatile.Read(ref _cameras);
                updated = [.. current.Where(camera => !ReferenceEquals(camera, e))];
            } while (!ReferenceEquals(
                         Interlocked.CompareExchange(ref _cameras, updated, current),
                         current));
            CameraDisconnected?.Invoke(this, [.. updated]);
        }

        protected virtual void OnCameraFault(List<ICamera> e)
        {
            CameraFault?.Invoke(this, e);
        }

        protected virtual void OnBarcodeScanned(BarcodeReadEventArgs e)
        {
            BarcodeScanned?.Invoke(this, e);
        }

        protected virtual void OnNotBarcodeHitEvent(BarcodeReadEventArgs e)
        {
            NotBarcodeHitEvent?.Invoke(this, e);
        }

        protected virtual void OnPanoramaCaptured(PanoramaCaptureEventArgs e)
        {
            PanoramaCaptured?.Invoke(this, e);
        }

        protected virtual void OnVolumeCaptured(VolumeCapturedEventArgs e)
        {
            VolumeCaptured?.Invoke(this, e);
        }

        protected virtual void OnRealTimeImage(RealTimeImageEventArgs e)
        {
            RealTimeImage?.Invoke(this, e);
        }

        protected virtual void OnCameraBound(CameraFinderItemInfoModel e)
        {
            CameraBound?.Invoke(this, e);
        }

        protected virtual void OnCameraParametersModified(List<CameraParametersModifiedEventArgs> e)
        {
            CameraParametersModified?.Invoke(this, e);
        }

        protected virtual void OnDeviceException(DeviceExceptionEventArgs e)
        {
            DeviceException?.Invoke(this, e);
        }

        protected virtual void OnCameraReleased(string e)
        {
            CameraReleased?.Invoke(this, e);
        }

        private ICamera? ConvertCamera(CameraInfo info)
        {
            switch (info.Brand)
            {
                case not null when (info.Brand.Contains("Hikrobot") || info.Brand.Contains("Hikvision")):
                    if (info.Model.Contains("MV-D"))
                    {
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

        private async Task<Dictionary<UsbCameraParameter, object>?> GetUsbCameraParameter(string serialNumber)
        {
            try
            {
                var usbCameraConfigInfoModel = await _usbCameraConfigRepository.
                    FirstOrDefault(f =>
                        f.SerialNumber.Equals(serialNumber));
                if (usbCameraConfigInfoModel is not null)
                {
                    var dictionary = new Dictionary<UsbCameraParameter, object>();
                    //曝光度
                    if (usbCameraConfigInfoModel.IsCustomExposureEnabled)
                    {
                        dictionary.Add(UsbCameraParameter.Exposure, usbCameraConfigInfoModel.Exposure);
                    }
                    //亮度
                    if (usbCameraConfigInfoModel.IsCustomBrightnessEnabled)
                    {
                        dictionary.Add(UsbCameraParameter.Brightness, usbCameraConfigInfoModel.Brightness);
                    }
                    //对比度
                    if (usbCameraConfigInfoModel.IsCustomContrastEnabled)
                    {
                        dictionary.Add(UsbCameraParameter.Contrast, usbCameraConfigInfoModel.Contrast);
                    }
                    //色调
                    if (usbCameraConfigInfoModel.IsCustomHueEnabled)
                    {
                        dictionary.Add(UsbCameraParameter.Hue, usbCameraConfigInfoModel.Hue);
                    }
                    //锐度
                    if (usbCameraConfigInfoModel.IsCustomSharpnessEnabled)
                    {
                        dictionary.Add(UsbCameraParameter.Sharpness, usbCameraConfigInfoModel.Sharpness);
                    }
                    //伽马值
                    if (usbCameraConfigInfoModel.IsCustomGammaEnabled)
                    {
                        dictionary.Add(UsbCameraParameter.Gamma, usbCameraConfigInfoModel.Gamma);
                    }
                    //白平衡
                    if (usbCameraConfigInfoModel.IsCustomWhiteBalanceEnabled)
                    {
                        dictionary.Add(UsbCameraParameter.WhiteBalance, usbCameraConfigInfoModel.WhiteBalance);
                    }
                    //背光补偿
                    if (usbCameraConfigInfoModel.IsCustomBacklightCompensationEnabled)
                    {
                        dictionary.Add(UsbCameraParameter.BklightComp, usbCameraConfigInfoModel.BklightComp);
                    }
                    return dictionary;
                }
            }
            catch (Exception e)
            {
            }

            return null;
        }

        private async Task<Dictionary<BarcodeReaderParameter, object>?> GetBarcodeReaderParameter()
        {
            var usbBarcodeReaderDto = await _settingsStore.GetAsync<UsbBarcodeReaderDto>("AlgorithmSettings") ??
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

        protected virtual void OnScaleConnected(ScaleConnectedEventArgs e)
        {
            ScaleConnected?.Invoke(this, e);
        }

        protected virtual void OnScaleDisconnected(ScaleDisconnectedEventArgs e)
        {
            ScaleDisconnected?.Invoke(this, e);
        }

        protected virtual void OnRealTimeWeight(RealTimeWeightEventArgs e)
        {
            RealTimeWeight?.Invoke(this, e);
        }

        protected virtual void OnStableWeight(StableWeightEventArgs e)
        {
            StableWeight?.Invoke(this, e);
        }

        protected virtual void OnWeightStabilized(WeightChangedEventArgs e)
        {
            WeightStabilized?.Invoke(this, e);
        }

        protected virtual void OnCameraException(DeviceExceptionEventArgs e)
        {
            CameraException?.Invoke(this, e);
        }

        protected virtual void OnOcrExceptionOccurred(OcrExceptionEventArgs e)
        {
            OcrExceptionOccurred?.Invoke(this, e);
        }

        protected virtual void OnOcrInitializationExceptionOccurred(OcrInitializationExceptionEventArgs e)
        {
            OcrInitializationExceptionOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// 仅在业务确实需要保存原分辨率图片或执行 OCR 时要求相机输出原图。
        /// </summary>
        private void ApplyImageOutputSettings(IEnumerable<ICamera> cameras)
        {
            var settings = Volatile.Read(ref _imageSettingsDto) ?? new ImageSettingsDto();
            foreach (var camera in cameras)
            {
                camera.IsOriginalImageOut = camera.BindingType switch
                {
                    CameraBindingType.OcrCamera => true,
                    CameraBindingType.ScannerCamera => settings.IsSaveBarcodeImage,
                    CameraBindingType.PanoramaCamera => settings.IsSavePanoramaImage,
                    CameraBindingType.VolumeCamera => settings.IsSaveVolumeImage,
                    _ => false
                };
            }
        }

        protected virtual void OnOcrContentRecognized(OcrResult e)
        {
            try
            {
                OcrContentRecognized?.Invoke(this, e);
            }
            finally
            {
                e.CropImage?.Dispose();
                e.CropImage = null;
            }
        }

        protected virtual void OnAuthenticationExceptionOccurred(AuthenticationExceptionEventArgs e)
        {
            AuthenticationExceptionOccurred?.Invoke(this, e);
        }

        protected virtual void OnWeightCleared(WeightChangedEventArgs e)
        {
            WeightCleared?.Invoke(this, e);
        }

        protected virtual void OnCameraStarted(CameraStartedEventArgs e)
        {
            CameraStarted?.Invoke(this, e);
        }

        protected virtual void OnBarCodeKeyReceived(KeyboardBarCodeReceivedEventArgs e)
        {
            BarCodeKeyReceived?.Invoke(this, e);
        }

        protected virtual void OnRealTimeKeyReceived(KeyboardRealTimeKeyEventArgs e)
        {
            RealTimeKeyReceived?.Invoke(this, e);
        }
    }
}
