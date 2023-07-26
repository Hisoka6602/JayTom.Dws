using System;
using GWCamera;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Device.Camera.SmartCamera {

    public class WayzimSmartCamera : ICamera {
        public string DeviceCode { get; } = string.Empty;
        public DeviceStatus Status { get; } = DeviceStatus.Uninitialized;
        public DeviceType Type => DeviceType.Camera;

        public Task<KeyValuePair<bool, string>> Reconnect() {
            //暂不写重连事件
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> Connect<T>(T connectParam) {
            //启动扫码
            try {
                GWCamera.CameraBLL.Instance.StartGrabber();
                OnConnected(this);
            }
            catch (Exception e) {
                return Task.FromResult(new KeyValuePair<bool, string>(false, e.Message));
            }
            return Task.FromResult(new KeyValuePair<bool, string>(true, string.Empty));
        }

        public void Dispose() {
            //释放相机资源
            GWCamera.CameraBLL.Instance.StopGrabber();
            GWCamera.CameraBLL.Instance.UpdateCamera -= InstanceOnUpdateCamera;
            GWCamera.CameraBLL.Instance.ReadCodeSystemStateEvent -= InstanceOnReadCodeSystemStateEvent;
            GWCamera.CameraBLL.Instance.GetGwReadCodeResultModelEvent -= InstanceOnGetGwReadCodeResultModelEvent;
            GWCamera.CameraBLL.Instance.GetGwReadCodeDelayImageModelEvent -= InstanceOnGetGwReadCodeDelayImageModelEvent;
            GWCamera.CameraBLL.Instance.CloseCamera();
        }

        public Task<KeyValuePair<bool, string>> Initialization() {
            //注册事件
            try {
                GWCamera.CameraBLL.Instance.UpdateCamera -= InstanceOnUpdateCamera;
                GWCamera.CameraBLL.Instance.UpdateCamera += InstanceOnUpdateCamera;
                GWCamera.CameraBLL.Instance.ReadCodeSystemStateEvent -= InstanceOnReadCodeSystemStateEvent;
                GWCamera.CameraBLL.Instance.ReadCodeSystemStateEvent += InstanceOnReadCodeSystemStateEvent;
                GWCamera.CameraBLL.Instance.GetGwReadCodeResultModelEvent -= InstanceOnGetGwReadCodeResultModelEvent;
                GWCamera.CameraBLL.Instance.GetGwReadCodeResultModelEvent += InstanceOnGetGwReadCodeResultModelEvent;
                GWCamera.CameraBLL.Instance.GetGwReadCodeDelayImageModelEvent -= InstanceOnGetGwReadCodeDelayImageModelEvent;
                GWCamera.CameraBLL.Instance.GetGwReadCodeDelayImageModelEvent += InstanceOnGetGwReadCodeDelayImageModelEvent;
                //开启相机 开启读码系统 这里参数请一定要传入,不要使用其他的InitCamera函数, true:狂扫模式 false:大件六面扫码光电触发模式
                GWCamera.CameraBLL.Instance.InitCamera(false);
                OnInitialized(this);
            }
            catch (Exception e) {
                return Task.FromResult(new KeyValuePair<bool, string>(false, JsonConvert.SerializeObject(e)));
            }

            return Task.FromResult(new KeyValuePair<bool, string>(true, string.Empty));
        }

        /// <summary>
        /// 延迟图像回调函数  比如大件六面扫模式下 相机有8个 但是再给出包裹数据结果的时候某个相机的图像还没有收到 那么
        /// 由于包裹数据需要及时给出,所以会通过GW_ReadCodeResultModel.ImageList给出已经收到的图像,然后通过这个回调函数
        /// 给出晚来的图像数据
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void InstanceOnGetGwReadCodeDelayImageModelEvent(GW_DelayImageModel obj) {
            Console.WriteLine(obj);
        }

        /// <summary>
        /// 记录条码系统状态
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void InstanceOnReadCodeSystemStateEvent(bool obj) {
            Console.WriteLine(obj);
        }

        /// <summary>
        /// 条码结果回调函数
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void InstanceOnGetGwReadCodeResultModelEvent(GW_ReadCodeResultModel obj) {
            if (obj.Barcode?.Any() == true) {
                for (var i = 0; i < obj.Barcode.Length; i++) {
                    Bitmap? bitmap = null;

                    var cameraName = obj.CameraNames[i];
                    var imageData = obj.ImageList?.Where(w => w.CameraName.Equals(cameraName) && w.ImageData.Length > 0)
                        ?.FirstOrDefault()?.ImageData;
                    if (imageData != null) {
                        bitmap = (Bitmap?)Image.FromStream(new MemoryStream(imageData));
                    }
                    OnBarcodeHitEvent(new BarcodeHitEventArgs() {
                        Barcode = obj.Barcode[i] ?? string.Empty,
                        ScanTime = DateTime.Now,
                        Image = bitmap,
                        CameraId = $"{cameraName ?? string.Empty}"
                    });
                }
            }
        }

        /// <summary>
        /// 单个相机状态
        /// </summary>
        /// <param name="cameraname"></param>
        /// <param name="state"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void InstanceOnUpdateCamera(string cameraname, bool state) {
            Console.WriteLine(cameraname);
        }

        public event EventHandler<IDevice>? Initialized;

        public event EventHandler<IDevice>? Connected;

        public event EventHandler<IDevice>? Disconnected;

        public event EventHandler<IDevice>? Reconnected;

        public event EventHandler<Exception>? Excepted;

        public string CameraName { get; } = string.Empty;
        public string CameraId { get; } = string.Empty;
        public float Framerate { get; } = 0;
        public string Brand => "中科微至";
        public CameraStatus CameraStatus { get; } = CameraStatus.Disconnected;
        public CameraType CameraType { get; } = CameraType.SmartCamera;
        public ConnectionType ConnectionType { get; } = ConnectionType.Ethernet;
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

        protected virtual async void OnInitialized(IDevice e) {
            await Task.Yield();
            Initialized?.Invoke(this, e);
        }

        protected virtual async void OnConnected(IDevice e) {
            await Task.Yield();
            Connected?.Invoke(this, e);
        }

        protected virtual async void OnBarcodeHitEvent(BarcodeHitEventArgs e) {
            await Task.Yield();
            BarcodeHitEvent?.Invoke(this, e);
        }
    }
}