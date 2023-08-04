using System;
using ImTools;
using System.Linq;
using System.Drawing;
using RTools_NTS.Util;
using System.Threading;
using JayTom.Dws.Device;
using System.Threading.Tasks;
using JayTom.Dws.Device.Camera;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using CameraType = JayTom.Dws.Client.Models.CameraType;
using ConnectionType = JayTom.Dws.Client.Models.ConnectionType;

namespace JayTom.Dws.Client.Service.Device {

    public class DeviceService : IDeviceService {
        private readonly ICamera _camera;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private readonly IPanoramaCameraConfigRepository _panoramaCameraConfigRepository;
        private readonly IVolumeCameraConfigRepository _volumeCameraConfigRepository;
        private List<string> CameraInitializationException { get; set; } = new();
        public bool RunningStatus { get; private set; } = false;

        public event EventHandler<List<ICamera>>? CameraInitialized;

        public event EventHandler<List<ICamera>>? CameraDisconnected;

        public event EventHandler<List<ICamera>>? CameraFault;

        public event EventHandler<BarcodeHitEventArgs>? BarcodeScanned;

        public event EventHandler<BarcodeHitEventArgs>? NotBarcodeHitEvent;

        public event EventHandler<PanoramaCaptureEventArgs>? PanoramaCaptured;

        public event EventHandler<VolumeCapturedEventArgs>? VolumeCaptured;

        public event EventHandler<RealTimeImageEventArgs>? RealTimeImage;

        public event EventHandler<List<CameraFinderItemInfoModel>>? CameraEnumerationRefreshed;

        public async Task<KeyValuePair<bool, string>> OnCameraEnumerationRefreshed(CancellationToken token = default) {
            //枚举SDK相机
            if (_camera.Status == DeviceStatus.Uninitialized) {
                return new KeyValuePair<bool, string>(false, "设备未初始化!");
            }
            try {
                var list = await _camera.RetrieveCamera(token);
                var itemInfoModels = list.Select(s => new CameraFinderItemInfoModel {
                    SerialNumber = s.SerialNumber,
                    Model = s.Model,
                    Name = s.CameraName,
                    IpAddress = s.IpAddress,
                    ConnectionType = (ConnectionType)s.ConnectionType,
                    CameraType = (CameraType)s.CameraType,
                    Version = s.Version
                })?.ToList();
                CameraEnumerationRefreshed?.Invoke(null, itemInfoModels ?? new List<CameraFinderItemInfoModel>());
                return new KeyValuePair<bool, string>(false, "相机检索成功");
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        public event EventHandler<CameraFinderItemInfoModel>? CameraBound;

        public async Task<KeyValuePair<bool, string>> OnCameraBound(CameraFinderItemInfoModel camera, CancellationToken token = default) {
            //如果运行中则不能解绑或者绑定
            await Task.Yield();
            /*if (RunningStatus) {
                return new KeyValuePair<bool, string>(false, $"设备运行中则不能解绑或者绑定!");
            }
            else {
                CameraBound?.Invoke(null, camera);
                return new KeyValuePair<bool, string>(true, string.Empty);
            }*/
            CameraBound?.Invoke(null, camera);
            return new KeyValuePair<bool, string>(true, string.Empty);
        }

        public event EventHandler<CameraFinderItemInfoModel>? CameraUnbound;

        public async Task<KeyValuePair<bool, string>> OnCameraUnbound(CameraFinderItemInfoModel camera, CancellationToken token = default) {
            await Task.Yield();
            /*if (RunningStatus) {
                return new KeyValuePair<bool, string>(false, $"设备运行中则不能解绑或者绑定!");
            }
            else {
                CameraUnbound?.Invoke(null, camera);
                return new KeyValuePair<bool, string>(true, string.Empty);
            }*/
            CameraUnbound?.Invoke(null, camera);
            return new KeyValuePair<bool, string>(true, string.Empty);
        }

        public event EventHandler<List<CameraParametersModifiedEventArgs>>? CameraParametersModified;

        public async Task<KeyValuePair<bool, string>> OnCameraParametersModified(List<CameraParametersModifiedEventArgs> camera, CancellationToken token = default) {
            await Task.Yield();
            /*if (RunningStatus) {
                return new KeyValuePair<bool, string>(false, $"设备运行中则不能解绑或者绑定!");
            }
            else {
                CameraParametersModified?.Invoke(null, camera);
                return new KeyValuePair<bool, string>(true, string.Empty);
            }*/
            CameraParametersModified?.Invoke(null, camera);
            return new KeyValuePair<bool, string>(true, string.Empty);
        }

        public event EventHandler<DeviceExceptionEventArgs>? DeviceException;

        public DeviceService(ICamera camera, IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IPanoramaCameraConfigRepository panoramaCameraConfigRepository,
            IVolumeCameraConfigRepository volumeCameraConfigRepository) {
            _camera = camera;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _panoramaCameraConfigRepository = panoramaCameraConfigRepository;
            _volumeCameraConfigRepository = volumeCameraConfigRepository;
            _camera.RealtimeImageEvent += delegate (object? sender, RealtimeImageEventArgs args) {
                OnRealTimeImage(new RealTimeImageEventArgs() {
                    Camera = args.Camera,
                    Image = args.Bitmap,
                });
            };
            _camera.BarcodeHitEvent += delegate (object? sender, BarcodeHitEventArgs args) {
                OnBarcodeScanned(args);
            };
            _camera.PanoramaCaptured += delegate (object? sender, PanoramaCaptureEventArgs args) {
            };
            _camera.NotBarcodeHitEvent += delegate (object? sender, BarcodeHitEventArgs args) {
                OnNotBarcodeHitEvent(args);
            };
            _camera.Disconnected += delegate (object? sender, IDevice device) {
                OnDeviceException(new DeviceExceptionEventArgs() {
                    Device = device,
                    ExceptionMessage = new Exception("设备断开!")
                });
            };
            _camera.Excepted += delegate (object? sender, Exception exception) {
                OnDeviceException(new DeviceExceptionEventArgs() {
                    Device = _camera,
                    ExceptionMessage = exception
                });
            };
            _camera.Connected += delegate (object? sender, IDevice device) {
            };
            _camera.Initialized += delegate (object? sender, IDevice device) {
            };
            _camera.Reconnected += delegate (object? sender, IDevice device) {
            };
            //初始化
            Initialization();
        }

        public async Task<KeyValuePair<bool, string>> Start(CancellationToken token = default) {
            await Task.Yield();
            //相机初始化
            //其他各项初始化
            if (_camera.Status == DeviceStatus.Connected) {
                return new KeyValuePair<bool, string>(true, "设备已连接,不需要重复连接");
            }
            if (_camera.Status == DeviceStatus.Initialized) {
                //后续可能需要填参数
                var (b, s) = await _camera.Connect(string.Empty);
                if (b) {
                    RunningStatus = true;
                    return new KeyValuePair<bool, string>(true, "设备已连接");
                }
                else {
                    OnDeviceException(new DeviceExceptionEventArgs() {
                        Device = _camera,
                        ExceptionMessage = new Exception(s)
                    });
                }
            }
            else {
                OnDeviceException(new DeviceExceptionEventArgs() {
                    Device = _camera,
                    ExceptionMessage = new Exception($"设备未初始化,设备状态:[{_camera.Status}]")
                });
            }
            return new KeyValuePair<bool, string>(false, "设备连接失败");
        }

        public async Task<KeyValuePair<bool, string>> Stop(CancellationToken token = default) {
            await Task.Yield();
            //各项停止和释放资源
            _camera?.Dispose();
            RunningStatus = false;
            return new KeyValuePair<bool, string>(true, "设备已释放");
        }

        public async void Initialization() {
            await Task.Yield();
            if (RunningStatus || _camera.Status != DeviceStatus.Uninitialized) {
                return;
            }
            await Task.Run(async () => {
                CameraInitializationException.Clear();
                var scannerCameraConfigInfoModels = await _barcodeScannerCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                var panoramaCameraConfigInfoModels = await _panoramaCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                var volumeCameraConfigInfoModels = await _volumeCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                var (key, value) = await _camera.Initialization();
                if (key) {
                    //开始
                    var (b, value1) = await _camera.Connect(string.Empty);
                    if (b) {
                        var cameras = new List<ICamera>();
                        var retrieveCamera = await _camera.RetrieveCamera();
                        OnDeviceException(new DeviceExceptionEventArgs() {
                            Device = _camera,
                            ExceptionMessage = new Exception($"返回:{retrieveCamera.Count}个相机")
                        });
                        cameras.AddRange(CheckAndAddCamera(retrieveCamera, scannerCameraConfigInfoModels?.Select(s => new BaseCameraConfigInfoModel {
                            Name = s.Name,
                            SerialNumber = s.SerialNumber,
                            Model = s.Model,
                            Version = s.Version,
                            IpAddress = s.IpAddress,
                            ConnectionType = s.ConnectionType,
                            CameraType = s.CameraType,
                        })?.ToList() ?? new List<BaseCameraConfigInfoModel>(), "扫码"));
                        cameras.AddRange(CheckAndAddCamera(retrieveCamera, panoramaCameraConfigInfoModels?.Select(s => new BaseCameraConfigInfoModel {
                            Name = s.Name,
                            SerialNumber = s.SerialNumber,
                            Model = s.Model,
                            Version = s.Version,
                            IpAddress = s.IpAddress,
                            ConnectionType = s.ConnectionType,
                            CameraType = s.CameraType,
                        })?.ToList() ?? new List<BaseCameraConfigInfoModel>(), "全景"));
                        cameras.AddRange(CheckAndAddCamera(retrieveCamera, volumeCameraConfigInfoModels?.Select(s => new BaseCameraConfigInfoModel {
                            Name = s.Name,
                            SerialNumber = s.SerialNumber,
                            Model = s.Model,
                            Version = s.Version,
                            IpAddress = s.IpAddress,
                            ConnectionType = s.ConnectionType,
                            CameraType = s.CameraType,
                        })?.ToList() ?? new List<BaseCameraConfigInfoModel>(), "体积"));

                        //显示绑定窗口(逻辑判断绑定:如果相机类型是智能相机，并且同一品牌，则算智能相机组，一组一个画面)
                        //枚举相机
                        //获取绑定相机
                        //传递初始化完成事件
                        //获取相机的绑定信息，如果至少绑定了一个全景相机，那么就至少有两个画面
                        OnCameraInitialized(cameras);
                        if (CameraInitializationException?.Any() == true) {
                            OnDeviceException(new DeviceExceptionEventArgs() {
                                Device = _camera,
                                ExceptionMessage = new Exception(string.Join(",", CameraInitializationException))
                            });
                        }
                    }
                    else {
                        OnDeviceException(new DeviceExceptionEventArgs() {
                            Device = _camera,
                            ExceptionMessage = new Exception(value1)
                        });
                    }
                }
                else {
                    OnDeviceException(new DeviceExceptionEventArgs() {
                        Device = _camera,
                        ExceptionMessage = new Exception(value)
                    });
                }
            });
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        private List<ICamera> CheckAndAddCamera(List<ICamera> sdkCameras, List<BaseCameraConfigInfoModel> configInfoModels, string cameraType) {
            var cameras = new List<ICamera>();
            foreach (var infoModel in configInfoModels) {
                var camera = sdkCameras.FirstOrDefault(f => f.SerialNumber.Equals(infoModel.SerialNumber));

                if (camera is not null && cameras?.Any(a => a.Brand == camera.Brand) != true) {
                    switch (cameraType) {
                        case "全景":
                            camera.CameraType = Dws.Device.Camera.CameraType.PanoramicCamera;
                            break;

                        case "体积":
                            camera.CameraType = Dws.Device.Camera.CameraType.ThreeDCamera;
                            break;
                    }
                    camera.CameraId = infoModel.SerialNumber;
                    cameras?.Add(camera);
                }
                else {
                    CameraInitializationException.Add($"{cameraType}相机:[名称:{infoModel.Name},序列号:{infoModel.SerialNumber}]未连接!");
                }
            }
            return cameras ?? new List<ICamera>();
        }

        protected virtual async void OnCameraInitialized(List<ICamera> e) {
            await Task.Yield();
            CameraInitialized?.Invoke(this, e);
        }

        protected virtual async void OnDeviceException(DeviceExceptionEventArgs e) {
            await Task.Yield();
            DeviceException?.Invoke(this, e);
        }

        protected virtual async void OnRealTimeImage(RealTimeImageEventArgs e) {
            await Task.Yield();
            RealTimeImage?.Invoke(this, e);
        }

        protected virtual async void OnBarcodeScanned(BarcodeHitEventArgs e) {
            await Task.Yield();
            BarcodeScanned?.Invoke(this, e);
        }

        protected virtual async void OnNotBarcodeHitEvent(BarcodeHitEventArgs e) {
            await Task.Yield();
            NotBarcodeHitEvent?.Invoke(this, e);
        }

        protected virtual async void OnPanoramaCaptured(PanoramaCaptureEventArgs e) {
            await Task.Yield();
            PanoramaCaptured?.Invoke(this, e);
        }
    }
}