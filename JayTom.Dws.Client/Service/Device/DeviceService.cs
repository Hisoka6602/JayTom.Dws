using System;
using ImTools;
using System.IO;
using System.Linq;
using System.Drawing;
using RTools_NTS.Util;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Camera;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Camera.Cameras.SmartCamera.Hikvision;
using CameraType = JayTom.Dws.Client.Models.CameraType;
using JayTom.Dws.Camera.Cameras.IndustrialCamera.Hikvision;
using ConnectionType = JayTom.Dws.Client.Models.ConnectionType;

namespace JayTom.Dws.Client.Service.Device {

    public class DeviceService : IDeviceService {
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private readonly IPanoramaCameraConfigRepository _panoramaCameraConfigRepository;
        private readonly IVolumeCameraConfigRepository _volumeCameraConfigRepository;
        private List<string> CameraInitializationException { get; set; } = new();
        private List<CameraInfo> _cameraInfos = new();
        private List<ICamera> _cameras = new();
        public bool RunningStatus { get; private set; } = false;
        public ScaleType ScaleType { get; }

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
            //枚举SDK相机
            //用所有SDK分别枚举，然后合并相同内容,再返回
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
                    CameraType = (CameraType)s.Type,
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

        public event EventHandler<string>? CameraReleased;

        public event EventHandler<ScaleConnectedEventArgs>? ScaleConnected;

        public event EventHandler<ScaleDisconnectedEventArgs>? ScaleDisconnected;

        public event EventHandler<RealTimeWeightEventArgs>? RealTimeWeight;

        public event EventHandler<StableWeightEventArgs>? StableWeight;

        public event EventHandler<DeviceExceptionEventArgs>? DeviceException;

        public DeviceService(IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IPanoramaCameraConfigRepository panoramaCameraConfigRepository,
            IVolumeCameraConfigRepository volumeCameraConfigRepository) {
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _panoramaCameraConfigRepository = panoramaCameraConfigRepository;
            _volumeCameraConfigRepository = volumeCameraConfigRepository;
            //初始化
            Initialization();
        }

        public async Task<KeyValuePair<bool, string>> Start(CancellationToken token = default) {
            await Task.Yield();
            if (RunningStatus) {
                return new KeyValuePair<bool, string>(true, "设备已连接,不需要重复连接");
            }
            foreach (var camera in _cameras) {
                var (key, value) = await camera.Start(string.Empty);
                if (!key) {
                    OnDeviceException(new DeviceExceptionEventArgs() {
                        ExceptionMessage = new Exception($"{value}")
                    });
                }
            }

            RunningStatus = true;
            return new KeyValuePair<bool, string>(true, "设备启动完成");
        }

        public async Task<KeyValuePair<bool, string>> Stop(CancellationToken token = default) {
            await Task.Yield();
            //各项停止和释放资源
            //_camera?.Dispose();
            RunningStatus = false;
            return new KeyValuePair<bool, string>(true, "设备已释放");
        }

        public async Task Initialization() {
            await Task.Yield();
            if (RunningStatus) {
                return;
            }
            await Task.Run(async () => {
                try {
                    CameraInitializationException.Clear();
                    var scannerCameraConfigInfoModels = await _barcodeScannerCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                    var panoramaCameraConfigInfoModels = await _panoramaCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                    var volumeCameraConfigInfoModels = await _volumeCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);

                    //枚举相机
                    var (key1, _) = await OnCameraEnumerationRefreshed();
                    if (key1) {
                        //定义、创建对象

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
                                    OnDeviceException(new DeviceExceptionEventArgs() {
                                        ExceptionMessage = args.Exception
                                    });
                                };
                                if (camera is IIndustrialCamera industrialCamera) {
                                    industrialCamera.BarcodeRead += delegate (object? sender, BarcodeReadEventArgs args) {
                                        OnBarcodeScanned(args);
                                    };
                                }
                                else if (camera is ISmartCamera smartCamera) {
                                    smartCamera.BarcodeReadTriggered +=
                                        delegate (object? sender, BarcodeTriggeredEventArgs args) {
                                            OnBarcodeScanned(args);
                                        };
                                    smartCamera.NotBarcodeHitEvent += delegate (object? sender, BarcodeReadEventArgs args) {
                                        OnBarcodeScanned(args);
                                    };
                                }
                                var (b, s) = await camera.Initialize(info);
                                if (!b) {
                                    CameraInitializationException.Add(s);
                                }
                                _cameras.Add(camera);
                            }
                        }
                    }
                    //智能相机分组
                    //如果品牌
                    //暂时不分组

                    OnDeviceException(new DeviceExceptionEventArgs() {
                        ExceptionMessage = new Exception($"_cameras.Count:{_cameras.Count}")
                    });
                    //绑定配置
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
                    //显示绑定窗口(逻辑判断绑定:如果相机类型是智能相机，并且同一品牌，则算智能相机组，一组一个画面)
                    //枚举相机
                    //获取绑定相机
                    //传递初始化完成事件
                    //获取相机的绑定信息，如果至少绑定了一个全景相机，那么就至少有两个画面
                    OnCameraInitialized(_cameras);
                    if (CameraInitializationException?.Any() == true) {
                        OnDeviceException(new DeviceExceptionEventArgs() {
                            ExceptionMessage = new Exception(string.Join(",", CameraInitializationException))
                        });
                    }
                }
                catch (Exception e) {
                    OnDeviceException(new DeviceExceptionEventArgs() {
                        ExceptionMessage = e
                    });
                }
            });
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 检查绑定
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

        protected virtual async void OnCameraInitialized(List<ICamera> e) {
            await Task.Yield();
            CameraInitialized?.Invoke(this, e);
        }

        protected virtual async void OnDeviceException(DeviceExceptionEventArgs e) {
            await Task.Yield();
            await File.AppendAllLinesAsync($"{AppDomain.CurrentDomain.BaseDirectory}\aa.txt", new[]
             {
                JsonConvert.SerializeObject(e)
            });
            DeviceException?.Invoke(this, e);
        }

        protected virtual async void OnRealTimeImage(RealTimeImageEventArgs e) {
            await Task.Yield();
            RealTimeImage?.Invoke(this, e);
        }

        protected virtual async void OnPanoramaCaptured(PanoramaCaptureEventArgs e) {
            await Task.Yield();
            PanoramaCaptured?.Invoke(this, e);
        }

        protected virtual async void OnCameraDisconnected(ICamera e) {
            await Task.Yield();
            _cameras.Remove(e);
            CameraDisconnected?.Invoke(this, _cameras);
        }

        protected virtual async void OnBarcodeScanned(BarcodeReadEventArgs e) {
            await Task.Yield();
            BarcodeScanned?.Invoke(this, e);
        }
    }
}