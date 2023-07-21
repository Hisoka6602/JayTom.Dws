using System;
using GWCamera;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Device.Camera.SmartCamera {

    public class WayzimSmartCamera : ICamera {
        public string DeviceCode { get; } = string.Empty;
        public DeviceStatus Status { get; } = DeviceStatus.Uninitialized;

        public Task<KeyValuePair<bool, string>> Reconnect() {
            //暂不写重连事件
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> Connect<T>(T connectParam) {
            //启动扫码
            throw new NotImplementedException();
        }

        public void Dispose() {
            //释放相机资源
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> Initialization() {
            //注册事件
            GWCamera.CameraBLL.Instance.UpdateCamera

            throw new NotImplementedException();
        }

        public event EventHandler<IDevice>? Initialized;

        public event EventHandler<IDevice>? Connected;

        public event EventHandler<IDevice>? Disconnected;

        public event EventHandler<IDevice>? Reconnected;

        public event EventHandler<Exception>? Excepted;

        public string CameraName { get; } = string.Empty;
        public string CameraId { get; } = string.Empty;
        public float Framerate { get; } = 0;
        public int BarcodeBorderSize { get; set; }
        public Color BarcodeBorderColor { get; set; }
        public bool IsShowBarcodeBorder { get; set; }
        public bool IsUseImageWatermark { get; set; }

        public event EventHandler<BarcodeHitEventArgs>? BarcodeHitEvent;

        public event EventHandler<Bitmap>? RealtimeImageEvent;

        public KeyValuePair<bool, string> SetFilterCondition<T>(T condition) {
            throw new NotImplementedException();
        }

        public KeyValuePair<bool, string> SetBarcodeType(BarcodeType type) {
            throw new NotImplementedException();
        }

        public KeyValuePair<bool, string> Pause() {
            //暂时画面
            throw new NotImplementedException();
        }

        public KeyValuePair<bool, string> Resume() {
            //回复画面
            throw new NotImplementedException();
        }

        public KeyValuePair<bool, string> SetConfiguration<T>(T configData) {
            throw new NotImplementedException();
        }
    }
}