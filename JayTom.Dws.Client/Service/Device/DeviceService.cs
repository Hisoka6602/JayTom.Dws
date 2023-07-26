using System;
using System.Drawing;
using JayTom.Dws.Device;
using System.Threading.Tasks;
using JayTom.Dws.Device.Camera;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Service.Device {

    public class DeviceService : IDeviceService {
        private readonly ICamera _camera;

        public event EventHandler<List<ICamera>>? CameraInitialized;

        public event EventHandler<List<ICamera>>? CameraDisconnected;

        public event EventHandler<List<ICamera>>? CameraFault;

        public event EventHandler<BarcodeHitEventArgs>? BarcodeScanned;

        public event EventHandler<VolumeCapturedEventArgs>? VolumeCaptured;

        public event EventHandler<RealTimeImageEventArgs>? RealTimeImage;

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
            _camera.Disconnected += delegate (object? sender, IDevice device) {
            };
            _camera.Excepted += delegate (object? sender, Exception exception) {
            };
            _camera.Initialized += delegate (object? sender, IDevice device) {
            };
            _camera.Reconnected += delegate (object? sender, IDevice device) {
            };
        }

        public async Task<KeyValuePair<bool, string>> Start() {
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
                    return new KeyValuePair<bool, string>(true, "设备初始化完成");
                }
            }
            else {
                OnDeviceException(new DeviceExceptionEventArgs() {
                    Device = _camera,
                    ExceptionMessage = new Exception(value)
                });
            }
            return new KeyValuePair<bool, string>(true, "设备初始化失败");
        }

        public async Task<KeyValuePair<bool, string>> Stop() {
            await Task.Yield();
            //各项停止和释放资源
            _camera?.Dispose();
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
    }
}