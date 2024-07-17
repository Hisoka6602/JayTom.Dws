using System;
using NetSDKCS;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Camera.Attributes.CameraAttributes;

namespace JayTom.Dws.Camera {

    /// <summary>
    /// 相机接口
    /// </summary>
    public interface ICamera : IDisposable {

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
        CameraBindingType BindingType { get; set; }

        /// <summary>
        /// 枚举相机
        /// </summary>
        Task<List<CameraInfo>?> EnumerateCameras();

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
        /// 实时图像事件
        /// </summary>
        event EventHandler<RealtimeImageEventArgs> RealtimeImage;

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
        /// 设置参数方法
        /// </summary>
        /// <param name="parameters">参数集合</param>
        void SetParameters(Dictionary<string, object> parameters);

        /// <summary>
        /// 是否开启实时图像
        /// </summary>
        public bool IsRealtimeImageEnabled { get; }

        /// <summary>
        /// 开启实时图像
        /// </summary>
        void StartRealTimeImage();

        /// <summary>
        /// 停止实时录像
        /// </summary>
        void StopRealTimeImage();

        /// <summary>
        /// 拍照回调事件
        /// </summary>
        public event EventHandler<PhotoTakenEventArgs> PhotoTaken;

        /// <summary>
        /// 拍照
        /// </summary>
        /// <returns></returns>
        Task TakePhotoAsync(string barcode, long barcodeTimestamp, CancellationToken cancellation = default);

        /// <summary>
        /// 拍照
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="barcodeTimestamp"></param>
        /// <param name="delay"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        Task TakePhotoAsync(string barcode, long barcodeTimestamp, TimeSpan delay, CancellationToken cancellation = default);

        /// <summary>
        /// 拍照延迟
        /// </summary>
        public int TakePhotoDelay { get; set; }
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
        /// 端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 是否已激活并可以使用
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// 是否支持Ocr算法
        /// </summary>
        public bool IsOcrSupported { get; set; } = false;

        /// <summary>
        /// 相机类型
        /// </summary>
        public CameraType Type { get; set; }

        /// <summary>
        /// 相机连接类型
        /// </summary>
        public CameraConnectionType ConnectionType { get; set; }

        /// <summary>
        /// 自定义名称
        /// </summary>
        public string CustomName { get; set; } = string.Empty;

        public override bool Equals(object? obj) {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var otherInfo = (CameraInfo)obj;
            return //Name == otherInfo.Name &&
                   //Brand == otherInfo.Brand &&
                   SerialNumber == otherInfo.SerialNumber /*&&
                   IpAddress == otherInfo.IpAddress &&
                   Port == otherInfo.Port &&
                   Version == otherInfo.Version &&
                   Model == otherInfo.Model*/;
        }

        public override int GetHashCode() {
            return SerialNumber.GetHashCode();
            /*return Name.GetHashCode()
                                    //^ Brand.GetHashCode()
                                    ^ SerialNumber.GetHashCode()
                                    ^ IpAddress.GetHashCode()
                                    ^ Port.GetHashCode()
                                    /*^ Version.GetHashCode()
                                    ^ Model.GetHashCode()#1#;*/
        }
    }

    /// <summary>
    /// 相机类型枚举
    /// </summary>
    public enum CameraType {

        /// <summary>
        /// 工业相机
        /// </summary>
        [Description("工业相机"), CameraFontIcon("\xe9f5")]
        IndustrialCamera = 0,

        /// <summary>
        /// 智能相机
        /// </summary>
        [Description("智能相机"), CameraFontIcon("\xe6ef")]
        SmartCamera = 1,

        /// <summary>
        /// 3D相机
        /// </summary>
        [Description("3D相机/体积相机"), CameraFontIcon("\xea1a")]
        ThreeDCamera = 2,

        /// <summary>
        /// 录像相机
        /// </summary>
        [Description("录像相机/安防"), CameraFontIcon("\xea0b")]
        VideoCamera = 3,

        /// <summary>
        /// Usb相机
        /// </summary>
        [Description("UsbCamera"), CameraFontIcon("\xe9f5")]
        UsbCamera = 4
    }

    /// <summary>
    /// 相机连接类型枚举
    /// </summary>
    public enum CameraConnectionType {

        /// <summary>
        /// USB连接
        /// </summary>
        [CameraFontIcon("\xe7c5"), Description("USB连接")]
        Usb = 0,

        /// <summary>
        /// 网口连接
        /// </summary>
        [CameraFontIcon("\xe631"), Description("网口连接")]
        Ethernet = 1,

        /// <summary>
        /// 串口连接
        /// </summary>
        [CameraFontIcon("\xe62c"), Description("串口连接")]
        SerialPort = 2,

        /// <summary>
        /// 蓝牙连接
        /// </summary>
        [CameraFontIcon("\xec4a"), Description("蓝牙连接")]
        Bluetooth = 3,

        /// <summary>
        /// Tcp连接
        /// </summary>
        [CameraFontIcon("\xe62f"), Description("Tcp连接")]
        Tcp = 4,

        /// <summary>
        /// 未知连接
        /// </summary>
        [CameraFontIcon("\xe71f"), Description("未知连接")]
        Unknown = 5
    }

    /// <summary>
    /// 相机绑定类型枚举
    /// </summary>
    [Flags]
    public enum CameraBindingType {

        /// <summary>
        /// 扫码相机
        /// </summary>
        [CameraFontIcon("\xe9f5"), CameraBackgroundColor("#4169E1"), Description("扫码相机")]
        ScannerCamera = 0,

        /// <summary>
        /// 全景相机
        /// </summary>
        [CameraFontIcon("\xe605"), CameraBackgroundColor("#FF4169E1"), Description("全景相机")]
        PanoramaCamera = 1,

        /// <summary>
        /// 体积相机
        /// </summary>
        [CameraFontIcon("\xea1a"), CameraBackgroundColor("#1E90FF"), Description("体积相机")]
        VolumeCamera = 2,

        /// <summary>
        /// Ocr相机
        /// </summary>
        [CameraFontIcon("\xe7a3"), CameraBackgroundColor("#FF8C00"), Description("Ocr识别")]
        OcrCamera = 3,
    }

    /// <summary>
    /// 相机状态
    /// </summary>
    public enum CameraStatus {

        /// <summary>
        /// 未初始化
        /// </summary>
        [CameraBackgroundColor("#A9A9A9"), CameraFontIcon("\xe612")]
        Uninitialized,

        /// <summary>
        /// 已连接
        /// </summary>
        [CameraBackgroundColor("#A9A9A9"), CameraFontIcon("\xe612")]
        Connected,

        /// <summary>
        /// 已初始化
        /// </summary>
        [CameraBackgroundColor("#A9A9A9"), CameraFontIcon("\xe612")]
        Initialized,

        /// <summary>
        /// 运行中
        /// </summary>
        [CameraBackgroundColor("#32CD32"), CameraFontIcon("\xe693")]
        Running,

        /// <summary>
        /// 未连接
        /// </summary>
        [CameraBackgroundColor("#A9A9A9"), CameraFontIcon("\xe612")]
        Disconnected,

        /// <summary>
        /// 故障
        /// </summary>
        [CameraBackgroundColor("#FF4500"), CameraFontIcon("\xe612")]
        Failure,

        /// <summary>
        /// 暂停中
        /// </summary>
        [CameraBackgroundColor("#FF8C00"), CameraFontIcon("\xea82")]
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
        /// 安防相机SDK
        /// </summary>
        SecurityCamera,

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

    /// <summary>
    /// 扫码过滤参数
    /// </summary>
    public class ScanCodeFilterParams {

        /// <summary>
        /// 扫码时间间隔
        /// </summary>
        public int ScanInterval { get; set; } = 500;

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string RegularExpression { get; set; } = string.Empty;

        /// <summary>
        /// 重复条码过滤数量
        /// </summary>
        public int DuplicateBarcodeFilterCount { get; set; }

        /// <summary>
        /// 过滤输出内容(为空则不输出)
        /// </summary>
        public string FilterOutContent { get; set; } = string.Empty;

        /// <summary>
        /// 过滤方式
        /// </summary>
        public BarCodeFilterMode BarCodeFilterMode { get; set; } = BarCodeFilterMode.None;

        /// <summary>
        /// 自定义正则表达式
        /// </summary>
        public List<string> CustomRegularExpressionItems { get; set; } = new();

        /// <summary>
        /// 是否使用正则替换
        /// </summary>
        public bool IsUseCustomRegexReplacement { get; set; }

        /// <summary>
        /// 是否使用过滤条码码种类
        /// </summary>
        public bool IsUseFilteredBarcodeTypes { get; set; }

        /// <summary>
        /// 正则替换项
        /// </summary>
        public List<CustomRegexReplacementItemInfo> CustomRegexReplacementItems { get; set; } = new();
    }

    public class CustomRegexReplacementItemInfo {

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string RegexPattern { get; set; } = string.Empty;

        /// <summary>
        /// 替换的内容
        /// </summary>
        public string ReplaceContent { get; set; } = string.Empty;
    }

    /// <summary>
    /// 过滤类别
    /// </summary>
    public enum BarCodeFilterMode {

        /// <summary>
        /// 不过滤
        /// </summary>
        None = 0,

        /// <summary>
        /// 常规过滤
        /// </summary>
        BasicFilter = 1,

        /// <summary>
        /// 自定义正则过滤
        /// </summary>
        CustomRegexFilter = 2,
    }
}