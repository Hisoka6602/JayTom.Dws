using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Device.KeyboardDevice {

    public interface IKeyboardDeviceManager : IDisposable {

        // 是否正在监听
        bool IsListening { get; }

        /// <summary>
        /// 正在监听的设备
        /// </summary>
        KeyboardDevice ListeningDevice { get; }

        /// <summary>
        /// 枚举键盘设备
        /// </summary>
        /// <returns></returns>
        Task<List<KeyboardDevice>> EnumerateKeyboardDevices();

        /// <summary>
        /// 启动监听指定的键盘设备
        /// </summary>
        /// <param name="device"></param>
        /// <param name="onDataReceived"></param>
        /// <returns></returns>
        Task<bool> StartListening(KeyboardDevice device, Action<string> onDataReceived);

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

    public class KeyboardDevice {
        public int VendorId { get; set; }
        public int ProductId { get; set; }
        public string? DeviceName { get; set; }
        public string? DevicePath { get; set; }
        public string? ManufacturerName { get; set; }
        public bool IsConnected { get; set; }
    }
}