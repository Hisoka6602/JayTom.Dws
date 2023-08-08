using System;
using DryIoc;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Camera;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using CameraType = JayTom.Dws.Camera.CameraType;
using JayTom.Dws.Camera.Cameras.SmartCamera.Hikvision;
using JayTom.Dws.Camera.Cameras.IndustrialCamera.Hikvision;

namespace JayTom.Dws.Client.Service.Device {

    public class DefaultDeviceService : IDeviceService {
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private readonly IPanoramaCameraConfigRepository _panoramaCameraConfigRepository;
        private readonly IVolumeCameraConfigRepository _volumeCameraConfigRepository;
        private List<string> CameraInitializationException { get; set; } = new();
        private List<CameraInfo> _cameraInfos = new();
        private List<ICamera> _cameras = new();
        private List<CameraParametersModifiedEventArgs> _cameraParameters = new();
        public bool RunningStatus { get; private set; } = false;

        public event EventHandler<List<ICamera>>? CameraInitialized;

        public event EventHandler<List<ICamera>>? CameraDisconnected;

        public event EventHandler<List<ICamera>>? CameraFault;

        public event EventHandler<BarcodeReadEventArgs>? BarcodeScanned;

        public event EventHandler<BarcodeReadEventArgs>? NotBarcodeHitEvent;

        public event EventHandler<PanoramaCaptureEventArgs>? PanoramaCaptured;

        public event EventHandler<VolumeCapturedEventArgs>? VolumeCaptured;

        public event EventHandler<RealTimeImageEventArgs>? RealTimeImage;

        public event EventHandler<List<CameraFinderItemInfoModel>>? CameraEnumerationRefreshed;

        public async Task<KeyValuePair<bool, string>> OnCameraEnumerationRefreshed(CancellationToken token = default) {
            await Task.Yield();
            try {
                var industrialCamera = new HikvisionIndustrialCamera();
                var infos = industrialCamera.EnumerateCameras();
                var smartCamera = new HikvisionSmartCamera();
                var cameraInfos = smartCamera.EnumerateCameras();

                _cameraInfos = infos?.Union(cameraInfos
                                            ?? new List<CameraInfo>())?.ToList() ?? new List<CameraInfo>();

                var itemInfoModels = _cameraInfos?.Select(s => new CameraFinderItemInfoModel {
                    SerialNumber = s.SerialNumber,
                    Model = s.Model,
                    Name = s.Name,
                    IpAddress = s.IpAddress,
                    ConnectionType = (ConnectionType)s.ConnectionType,
                    CameraType = (JayTom.Dws.Client.Models.CameraType)s.Type,
                    Version = s.Version
                })?.ToList();
                CameraEnumerationRefreshed?.Invoke(null, itemInfoModels ?? new List<CameraFinderItemInfoModel>());
                return new KeyValuePair<bool, string>(true, "相机检索成功");
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        public event EventHandler<CameraFinderItemInfoModel>? CameraBound;

        public DefaultDeviceService(IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IPanoramaCameraConfigRepository panoramaCameraConfigRepository,
            IVolumeCameraConfigRepository volumeCameraConfigRepository) {
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _panoramaCameraConfigRepository = panoramaCameraConfigRepository;
            _volumeCameraConfigRepository = volumeCameraConfigRepository;
        }

        public async Task<KeyValuePair<bool, string>> OnCameraBound(CameraFinderItemInfoModel camera, CancellationToken token = default) {
            await Task.Yield();
            if (RunningStatus) {
                return new KeyValuePair<bool, string>(false, $"设备运行中则不能解绑或者绑定!");
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
                return new KeyValuePair<bool, string>(false, $"设备运行中则不能解绑或者绑定!");
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
                return new KeyValuePair<bool, string>(false, $"设备运行中则不能解绑或者绑定!");
            }
            else {
                _cameras.Clear();
                var scannerCameraConfigInfoModels = await _barcodeScannerCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                var panoramaCameraConfigInfoModels = await _panoramaCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                var volumeCameraConfigInfoModels = await _volumeCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                //保存绑定参数
                _cameraParameters.Clear();
                _cameraParameters.AddRange(scannerCameraConfigInfoModels?.Select(s => new CameraParametersModifiedEventArgs {
                    Type = BoundCameraType.BarcodeScannerCamera,
                    Parameters = s
                })?.ToList() ?? new List<CameraParametersModifiedEventArgs>());
                _cameraParameters.AddRange(panoramaCameraConfigInfoModels?.Select(s => new CameraParametersModifiedEventArgs {
                    Type = BoundCameraType.PanoramicCamera,
                    Parameters = s
                })?.ToList() ?? new List<CameraParametersModifiedEventArgs>());
                _cameraParameters.AddRange(volumeCameraConfigInfoModels?.Select(s => new CameraParametersModifiedEventArgs {
                    Type = BoundCameraType.VolumeCamera,
                    Parameters = s
                })?.ToList() ?? new List<CameraParametersModifiedEventArgs>());

                CameraParametersModified?.Invoke(null, camera);
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
        }

        public event EventHandler<string>? CameraReleased;

        public event EventHandler<DeviceExceptionEventArgs>? DeviceException;

        public async Task<KeyValuePair<bool, string>> Start(CancellationToken token = default) {
            //在这里初始化
            await Initialization();
            //启动(逐个相机启动)
            foreach (var camera in _cameras) {
                await camera.Start(string.Empty);
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

            await Task.Run(async () => {
                CameraInitializationException.Clear();
                _cameras.Clear();
                var scannerCameraConfigInfoModels = await _barcodeScannerCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                var panoramaCameraConfigInfoModels = await _panoramaCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                var volumeCameraConfigInfoModels = await _volumeCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                //保存绑定参数
                _cameraParameters.Clear();
                _cameraParameters.AddRange(scannerCameraConfigInfoModels?.Select(s => new CameraParametersModifiedEventArgs {
                    Type = BoundCameraType.BarcodeScannerCamera,
                    Parameters = s
                })?.ToList() ?? new List<CameraParametersModifiedEventArgs>());
                _cameraParameters.AddRange(panoramaCameraConfigInfoModels?.Select(s => new CameraParametersModifiedEventArgs {
                    Type = BoundCameraType.PanoramicCamera,
                    Parameters = s
                })?.ToList() ?? new List<CameraParametersModifiedEventArgs>());
                _cameraParameters.AddRange(volumeCameraConfigInfoModels?.Select(s => new CameraParametersModifiedEventArgs {
                    Type = BoundCameraType.VolumeCamera,
                    Parameters = s
                })?.ToList() ?? new List<CameraParametersModifiedEventArgs>());
                //过滤绑定
                var (key, value) = await OnCameraEnumerationRefreshed();
                if (key) {
                    foreach (var info in _cameraInfos) {
                        ICamera? camera = null;
                        if (info.Brand.Contains("Hikrobot") && info.Model.Contains("MV-ID")) {
                            //海康智能相机
                            camera = new HikvisionSmartCamera();
                        }
                        else if (info.Brand.Contains("Hikrobot") && info.Model.Contains("MV-PD")) {
                            //海康工业相机
                            camera = new HikvisionIndustrialCamera();
                        }
                        else if (info.Brand.Contains("Dahua") && info.Model.Contains("DH-MV")) {
                            //大华智能相机
                        }
                        else if (info.Brand.Contains("Dahua")) {
                            //大华工业相机
                        }
                        if (camera is not null) {
                            //注册事件
                            camera.CameraDisconnected += delegate (object? sender, CameraConnectionEventArgs args) {
                                if (sender is ICamera mCamera) {
                                    OnCameraDisconnected(mCamera);
                                }
                            };
                            camera.CameraExceptionOccurred += delegate (object? sender, CameraExceptionEventArgs args) {
                                string mCameraInfo = string.Empty;
                                if (sender is ICamera mCamera) {
                                    mCameraInfo =
                                        $"ID:{mCamera.Info?.Id},SerialNumber:{mCamera?.Info?.SerialNumber},SdkType:{mCamera?.SdkType}";
                                }
                                NLog.LogManager.GetCurrentClassLogger().Error($"{JsonConvert.SerializeObject(args.Exception)}");
                                OnDeviceException(new DeviceExceptionEventArgs() {
                                    ExceptionMessage = new Exception($"{mCameraInfo}-{args.Exception?.Message}")
                                });
                            };
                            if (camera is IIndustrialCamera industrialCamera) {
                                industrialCamera.BarcodeRead += delegate (object? sender, BarcodeReadEventArgs args) {
                                    OnBarcodeScanned(args);
                                };
                                industrialCamera.PhotoTaken += delegate (object? sender, PhotoTakenEventArgs args) {
                                    OnPanoramaCaptured(new PanoramaCaptureEventArgs() {
                                        CameraSerialNumber = args.CameraSerialNumber,
                                        Image = args.Image,
                                        PhotoTime = args.PhotoTime,
                                        Timestamp = args.Timestamp
                                    });
                                };
                            }
                            else if (camera is ISmartCamera smartCamera) {
                                smartCamera.BarcodeReadTriggered +=
                                    delegate (object? sender, BarcodeTriggeredEventArgs args) {
                                        OnBarcodeScanned(args);
                                    };
                                smartCamera.NotBarcodeHitEvent += delegate (object? sender, BarcodeReadEventArgs args) {
                                    OnNotBarcodeHitEvent(args);
                                };
                            }

                            //在这里还需要各自SDK枚举
                            var cameraInfo = camera.EnumerateCameras()?.FirstOrDefault(f => f.SerialNumber.Equals(info.SerialNumber));
                            if (cameraInfo is not null) {
                                //注册事件
                                var (b, s) = await camera.Initialize(cameraInfo);
                                if (!b) {
                                    CameraInitializationException.Add(s);
                                }
                                else {
                                    _cameras.Add(camera);
                                }
                            }
                        }
                    }
                }
                //判断绑定
                var cameras = new List<ICamera>();
                cameras.AddRange(CheckAndAddCamera(_cameras, scannerCameraConfigInfoModels?.Select(s => new BaseCameraConfigInfoModel {
                    Name = s.Name,
                    SerialNumber = s.SerialNumber,
                    Model = s.Model,
                    Version = s.Version,
                    IpAddress = s.IpAddress,
                    ConnectionType = s.ConnectionType,
                    CameraType = s.CameraType,
                })?.ToList() ?? new List<BaseCameraConfigInfoModel>(), "扫码"));
                cameras.AddRange(CheckAndAddCamera(_cameras, panoramaCameraConfigInfoModels?.Select(s => new BaseCameraConfigInfoModel {
                    Name = s.Name,
                    SerialNumber = s.SerialNumber,
                    Model = s.Model,
                    Version = s.Version,
                    IpAddress = s.IpAddress,
                    ConnectionType = s.ConnectionType,
                    CameraType = s.CameraType,
                })?.ToList() ?? new List<BaseCameraConfigInfoModel>(), "全景"));
                cameras.AddRange(CheckAndAddCamera(_cameras, volumeCameraConfigInfoModels?.Select(s => new BaseCameraConfigInfoModel {
                    Name = s.Name,
                    SerialNumber = s.SerialNumber,
                    Model = s.Model,
                    Version = s.Version,
                    IpAddress = s.IpAddress,
                    ConnectionType = s.ConnectionType,
                    CameraType = s.CameraType,
                })?.ToList() ?? new List<BaseCameraConfigInfoModel>(), "体积"));
                _cameras = cameras;
                OnCameraInitialized(_cameras);
            });
        }

        public void Dispose() {
            try {
                for (int i = _cameras.Count - 1; i >= 0; i--) {
                    var serialNumber = _cameras[i]?.Info?.SerialNumber ?? string.Empty;
                    _cameras[i]?.Dispose();
                    OnCameraReleased(serialNumber);
                }
            }
            catch (Exception e) {
                OnDeviceException(new DeviceExceptionEventArgs() {
                    ExceptionMessage = new Exception($"释放设备异常:{e.Message}")
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
            await Task.Yield();
            _cameras.Remove(e);
            CameraDisconnected?.Invoke(this, _cameras);
        }

        protected virtual async void OnCameraFault(List<ICamera> e) {
            await Task.Yield();
            CameraFault?.Invoke(this, e);
        }

        protected virtual async void OnBarcodeScanned(BarcodeReadEventArgs e) {
            await Task.Yield();
            BarcodeScanned?.Invoke(this, e);
            //判断如果需要有全景相机则触发
            var cameras = _cameras?.Where(w =>
                w is { BindingType: CameraBindingType.PanoramicCamera, SdkType: SdkType.IndustrialCameraSdk })?.ToList();
            if (cameras?.Any() == true) {
                foreach (var camera in cameras) {
                    if (camera is IIndustrialCamera industrialCamera) {
                        Task.Run(async () => {
                            var delayTime = _cameraParameters
                                .Where(w => w.Type == BoundCameraType.PanoramicCamera
                                           && w.Parameters is PanoramaCameraConfigInfoModel)
                                ?.Select(s => (PanoramaCameraConfigInfoModel)s.Parameters!)
                                ?.ToList()?.FirstOrDefault(f => f.SerialNumber.Equals(camera.Info.SerialNumber))
                                ?.CaptureDelayTime ?? 500;
                            await Task.Delay(delayTime);
                            //等待
                            await industrialCamera.TakePhotoAsync();
                        });
                    }
                }
            }
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

        /// <summary>
        /// 判断绑定
        /// </summary>
        /// <param name="sdkCameras"></param>
        /// <param name="configInfoModels"></param>
        /// <param name="cameraType"></param>
        /// <returns></returns>
        private List<ICamera> CheckAndAddCamera(List<ICamera> sdkCameras, List<BaseCameraConfigInfoModel> configInfoModels, string cameraType) {
            var cameras = new List<ICamera>();
            foreach (var infoModel in configInfoModels) {
                var camera = sdkCameras.FirstOrDefault(f => f?.Info?.SerialNumber?.Equals(infoModel.SerialNumber) == true);

                if (camera is not null && cameras?.Any(a => a?.Info?.SerialNumber == camera?.Info?.SerialNumber) != true) {
                    switch (cameraType) {
                        case "扫码":
                            camera.BindingType = CameraBindingType.ScannerCamera;
                            break;

                        case "全景":
                            camera.BindingType = CameraBindingType.PanoramicCamera;
                            if (camera.Info != null) camera.Info.Type = CameraType.PanoramicCamera;
                            break;

                        case "体积":
                            camera.BindingType = CameraBindingType.VolumeCamera;
                            break;
                    }
                    cameras?.Add(camera);
                }
                else {
                    CameraInitializationException.Add($"{cameraType}相机:[名称:{infoModel.Name},序列号:{infoModel.SerialNumber}]未连接!");
                }
            }
            return cameras ?? new List<ICamera>();
        }

        protected virtual async void OnCameraReleased(string e) {
            await Task.Yield();
            CameraReleased?.Invoke(this, e);
        }
    }
}