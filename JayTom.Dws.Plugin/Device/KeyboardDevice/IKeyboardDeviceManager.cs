using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using KeyboardDeviceInfo = JayTom.Dws.Abstractions.Devices.KeyboardDevice;

namespace JayTom.Dws.Plugin.Device.KeyboardDevice {

    public interface IKeyboardDeviceManager : IDisposable {

        /// <summary>
        /// 条码返回事件
        /// </summary>
        event EventHandler<KeyboardBarCodeReceivedEventArgs> BarCodeReceived;

        /// <summary>
        /// 实时按键事件
        /// </summary>
        event EventHandler<KeyboardRealTimeKeyEventArgs> RealTimeKeyReceived;

        // 是否正在监听
        bool IsListening { get; }

        /// <summary>
        /// 正在监听的设备
        /// </summary>
        KeyboardDeviceInfo ListeningDevice { get; }

        /// <summary>
        /// 枚举键盘设备
        /// </summary>
        /// <returns></returns>
        Task<List<KeyboardDeviceInfo>> EnumerateKeyboardDevices();

        /// <summary>
        /// 启动监听指定的键盘设备
        /// </summary>
        /// <param name="device"></param>

        /// <returns></returns>
        Task<bool> StartListening(KeyboardDeviceInfo device);

        /// <summary>
        /// 停止监听指定的键盘设备
        /// </summary>
        void StopListening();

        /// <summary>
        /// 设置数据过滤规则
        /// </summary>
        /// <param name="regexPattern"></param>
        void SetFilterRule(string regexPattern);
    }

    public class KeyboardRealTimeKeyEventArgs : EventArgs {

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime ScanTime { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public string Data { get; set; } = string.Empty;

        /// <summary>
        /// 键值
        /// </summary>
        public int VirutalKey { get; set; }

        /// <summary>
        /// 扫描值
        /// </summary>
        public int ScanCode { get; set; }

        /// <summary>
        /// 设备
        /// </summary>
        public KeyboardDeviceInfo? Device { get; set; }
    }

    public class KeyboardBarCodeReceivedEventArgs : EventArgs {

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime ScanTime { get; set; }

        /// <summary>
        /// 条码时间戳
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 设备
        /// </summary>
        public KeyboardDeviceInfo? Device { get; set; }
    }

}
