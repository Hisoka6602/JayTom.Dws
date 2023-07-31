using System;
using System.Linq;
using System.Drawing;
using System.Threading;
using JayTom.Dws.Device;
using System.Threading.Tasks;
using JayTom.Dws.Device.Camera;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.Cameras;
using CameraType = JayTom.Dws.Client.Models.CameraType;
using ConnectionType = JayTom.Dws.Client.Models.ConnectionType;

namespace JayTom.Dws.Client.Service.Device {

    public class DeviceService : IDeviceService {
        private readonly ICamera _camera;

        public bool RunningStatus { get; private set; } = false;

        public event EventHandler<List<ICamera>>? CameraInitialized;

        public event EventHandler<List<ICamera>>? CameraDisconnected;

        public event EventHandler<List<ICamera>>? CameraFault;

        public event EventHandler<BarcodeHitEventArgs>? BarcodeScanned;

        public event EventHandler<BarcodeHitEventArgs>? NotBarcodeHitEvent;

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
            return new KeyValuePair<bool, string>(true, string.Empty);
        }

        public event EventHandler<DeviceExceptionEventArgs>? DeviceException;

        public DeviceService(ICamera camera) {
            _camera = camera;
            _camera.RealtimeImageEvent += delegate (object? sender, Bitmap bitmap) {
                OnRealTimeImage(new RealTimeImageEventArgs() {
                    Camera = _camera,
                    Image = bitmap,
                });
            };
            _camera.BarcodeHitEvent += delegate (object? sender, BarcodeHitEventArgs args) {
                OnBarcodeScanned(args);
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
            _camera.Initialized += delegate (object? sender, IDevice device) {
            };
            _camera.Reconnected += delegate (object? sender, IDevice device) {
            };
        }

        public async Task<KeyValuePair<bool, string>> Start(CancellationToken token = default) {
            await Task.Yield();
            //相机初始化
            //其他各项初始化
            var (key, value) = await _camera.Initialization();
            if (key) {
                //后续可能需要填参数
                var (b, s) = await _camera.Connect(string.Empty);
                if (b) {
                    OnCameraInitialized(new List<ICamera>()
                    {
                        _camera
                    });
                    RunningStatus = true;
                    return new KeyValuePair<bool, string>(true, "设备初始化完成");
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
                    ExceptionMessage = new Exception(value)
                });
            }
            return new KeyValuePair<bool, string>(false, "设备初始化失败");
        }

        public async Task<KeyValuePair<bool, string>> Stop(CancellationToken token = default) {
            await Task.Yield();
            //各项停止和释放资源
            _camera?.Dispose();
            RunningStatus = false;
            return new KeyValuePair<bool, string>(true, "设备已释放");
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
    }
}