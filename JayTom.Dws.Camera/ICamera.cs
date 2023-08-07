using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Camera {

    /// <summary>
    /// 相机接口
    /// </summary>
    public interface ICamera {

        /// <summary>
        /// 相机信息
        /// </summary>
        public CameraInfo? Info { get; }

        /// <summary>
        /// SDK类型
        /// </summary>
        public SdkType SdkType { get; }

        /// <summary>
        /// SDK名称
        /// </summary>
        public string SdkName { get; }

        /// <summary>
        /// 是否原图输出
        /// </summary>
        public bool IsOriginalImageOut { get; set; }

        /// <summary>
        /// 相机状态
        /// </summary>
        CameraStatus Status { get; }

        /// <summary>
        /// 相机绑定类型
        /// </summary>
        CameraBindingType BindingType { get; }

        /// <summary>
        /// 枚举相机
        /// </summary>
        List<CameraInfo>? EnumerateCameras();

        /// <summary>
        /// 相机异常事件
        /// </summary>
        event EventHandler<CameraExceptionEventArgs> CameraExceptionOccurred;

        /// <summary>
        /// 相机断开事件
        /// </summary>
        event EventHandler<CameraConnectionEventArgs> CameraDisconnected;

        /// <summary>
        /// 相机初始化完成事件
        /// </summary>
        event EventHandler<CameraInitializedEventArgs> CameraInitialized;

        /// <summary>
        /// 相机启动事件
        /// </summary>
        event EventHandler<CameraStartedEventArgs> CameraStarted;

        /// <summary>
        /// 相机停止事件
        /// </summary>
        event EventHandler<CameraStoppedEventArgs> CameraStopped;

        /// <summary>
        /// 相机注销事件
        /// </summary>
        event EventHandler<CameraUnregisteredEventArgs> CameraUnregistered;

        /// <summary>
        /// 初始化方法
        /// </summary>
        Task<KeyValuePair<bool, string>> Initialize(object param);

        /// <summary>
        /// 启动相机
        /// </summary>
        Task<KeyValuePair<bool, string>> Start(object param);

        /// <summary>
        /// 停止相机
        /// </summary>
        Task<KeyValuePair<bool, string>> Stop();

        /// <summary>
        /// 注销相机
        /// </summary>
        void Dispose();

        /// <summary>
        /// 设置参数方法
        /// </summary>
        /// <param name="parameters">参数集合</param>
        void SetParameters(Dictionary<string, object> parameters);
    }

    /// <summary>
    /// 相机硬件信息
    /// </summary>
    public class CameraInfo {

        /// <summary>
        /// 相机Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 相机名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 相机品牌
        /// </summary>
        public string Brand { get; set; } = string.Empty;

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 相机Ip地址
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 相机版本号
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 相机型号
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// 相机类型
        /// </summary>
        public CameraType Type { get; set; }

        /// <summary>
        /// 相机连接类型
        /// </summary>
        public CameraConnectionType ConnectionType { get; set; }
    }

    /// <summary>
    /// 相机类型枚举
    /// </summary>
    public enum CameraType {

        /// <summary>
        /// 工业相机
        /// </summary>
        IndustrialCamera,

        /// <summary>
        /// 智能相机
        /// </summary>
        SmartCamera,

        /// <summary>
        /// 体积相机
        /// </summary>
        VolumeCamera,

        /// <summary>
        /// 录像相机
        /// </summary>
        VideoCamera,

        /// <summary>
        /// 普通相机
        /// </summary>
        StandardCamera
    }

    /// <summary>
    /// 相机连接类型枚举
    /// </summary>
    public enum CameraConnectionType {

        /// <summary>
        /// USB连接
        /// </summary>
        Usb = 0,

        /// <summary>
        /// 网口连接
        /// </summary>
        Ethernet = 1,

        /// <summary>
        /// 串口连接
        /// </summary>
        SerialPort = 2,

        /// <summary>
        /// 蓝牙连接
        /// </summary>
        Bluetooth = 3,

        /// <summary>
        /// Tcp连接
        /// </summary>
        Tcp = 4,

        /// <summary>
        /// 未知连接
        /// </summary>
        Unknown = 5
    }

    /// <summary>
    /// 相机绑定类型枚举
    /// </summary>
    public enum CameraBindingType {

        /// <summary>
        /// 扫码相机
        /// </summary>
        ScannerCamera = 0,

        /// <summary>
        /// 全景相机
        /// </summary>
        PanoramicCamera = 1,

        /// <summary>
        /// 录像相机
        /// </summary>
        VideoCamera = 2
    }

    /// <summary>
    /// 相机状态
    /// </summary>
    public enum CameraStatus {

        /// <summary>
        /// 未初始化
        /// </summary>
        Uninitialized,

        /// <summary>
        /// 已连接
        /// </summary>
        Connected,

        /// <summary>
        /// 已初始化
        /// </summary>
        Initialized,

        /// <summary>
        /// 运行中
        /// </summary>
        Running,

        /// <summary>
        /// 未连接
        /// </summary>
        Disconnected,

        /// <summary>
        /// 故障
        /// </summary>
        Failure,

        /// <summary>
        /// 暂停中
        /// </summary>
        Paused
    }

    /// <summary>
    /// SDK类型
    /// </summary>
    public enum SdkType {

        /// <summary>
        /// 智能相机SDK
        /// </summary>
        SmartCameraSdk,

        /// <summary>
        /// 工业相机SDK
        /// </summary>
        IndustrialCameraSdk,

        /// <summary>
        /// 体积相机SDK
        /// </summary>
        VolumeCameraSdk,

        /// <summary>
        /// 录像相机SDK
        /// </summary>
        VideoCameraSdk,

        /// <summary>
        /// 其他SDK
        /// </summary>
        OtherSdk
    }

    /// <summary>
    /// 相机信息事件参数
    /// </summary>
    public class CameraInfoEventArgs : EventArgs {
        public CameraInfo? CameraInfo { get; set; }
    }

    /// <summary>
    /// 相机异常事件参数
    /// </summary>
    public class CameraExceptionEventArgs : EventArgs {
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// 相机连接事件参数
    /// </summary>
    public class CameraConnectionEventArgs : EventArgs {
        public CameraInfo? CameraInfo { get; set; }
    }

    /// <summary>
    /// 相机初始化完成事件参数
    /// </summary>
    public class CameraInitializedEventArgs : EventArgs {
        public CameraInfo? CameraInfo { get; set; }
    }

    /// <summary>
    /// 相机启动事件参数
    /// </summary>
    public class CameraStartedEventArgs : EventArgs {
        public CameraInfo? CameraInfo { get; set; }
    }

    /// <summary>
    /// 相机停止事件参数
    /// </summary>
    public class CameraStoppedEventArgs : EventArgs {
        public CameraInfo? CameraInfo { get; set; }
    }

    /// <summary>
    /// 相机注销事件参数
    /// </summary>
    public class CameraUnregisteredEventArgs : EventArgs {
        public CameraInfo? CameraInfo { get; set; }
    }
}