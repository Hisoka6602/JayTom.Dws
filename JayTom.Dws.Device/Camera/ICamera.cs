using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Device.Camera {

    /// <summary>
    /// 相机
    /// </summary>
    public interface ICamera : IDevice {

        /// <summary>
        /// 相机名称
        /// </summary>
        string CameraName { get; }

        /// <summary>
        /// 相机Id
        /// </summary>
        string CameraId { get; }

        /// <summary>
        /// 相机帧率
        /// </summary>
        float Framerate { get; }

        /// <summary>
        /// 条码边框大小
        /// </summary>
        int BarcodeBorderSize { get; set; }

        /// <summary>
        /// 边框颜色
        /// </summary>
        Color BarcodeBorderColor { get; set; }

        /// <summary>
        /// 是否显示边框
        /// </summary>
        bool IsShowBarcodeBorder { get; set; }

        /// <summary>
        /// 是否添加图片水印
        /// </summary>
        bool IsUseImageWatermark { get; set; }

        /// <summary>
        /// 相机品牌
        /// </summary>
        string Brand { get; }

        /// <summary>
        /// 相机状态
        /// </summary>
        public CameraStatus CameraStatus { get; }

        /// <summary>
        /// 相机类型
        /// </summary>
        public CameraType CameraType { get; }

        /// <summary>
        /// 连接类型
        /// </summary>
        public ConnectionType ConnectionType { get; }

        /// <summary>
        /// 扫到条码
        /// </summary>
        event EventHandler<BarcodeHitEventArgs> BarcodeHitEvent;

        /// <summary>
        /// 实时图片
        /// </summary>
        event EventHandler<Bitmap> RealtimeImageEvent;

        /// <summary>
        /// 设置过滤条件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="condition"></param>
        /// <returns></returns>
        KeyValuePair<bool, string> SetFilterCondition<T>(T condition);

        /// <summary>
        /// 设置条码种类
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        KeyValuePair<bool, string> SetBarcodeType(BarcodeType type);

        /// <summary>
        /// 暂停
        /// </summary>
        /// <returns></returns>
        KeyValuePair<bool, string> Pause();

        /// <summary>
        /// 恢复
        /// </summary>
        /// <returns></returns>
        KeyValuePair<bool, string> Resume();

        /// <summary>
        /// 设置配置
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configData"></param>
        /// <returns></returns>
        KeyValuePair<bool, string> SetConfiguration<T>(T configData);
    }

    public class BarcodeHitEventArgs : EventArgs {

        /// <summary>
        /// 图片
        /// </summary>
        public Bitmap? Image { get; set; }

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime ScanTime { get; set; }

        /// <summary>
        /// 缩略图
        /// </summary>
        public Bitmap? ThumbImage { get; set; }

        /// <summary>
        /// 相机Id
        /// </summary>
        public string CameraId { get; set; } = string.Empty;

        /// <summary>
        /// 长
        /// </summary>
        public float Length { get; set; }

        /// <summary>
        /// 宽
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        /// 高
        /// </summary>
        public float Height { get; set; }

        /// <summary>
        /// 体积
        /// </summary>
        public float Volume { get; set; }

        /// <summary>
        /// 包裹时间戳
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 区域坐标
        /// </summary>
        public Point[]? AreaCoords { get; set; }

        /// <summary>
        /// 相机类型
        /// </summary>
        public CameraType CameraType { get; set; }

        /// <summary>
        /// 连接类型
        /// </summary>
        public ConnectionType ConnectionType { get; set; }
    }

    [Flags]
    public enum BarcodeType {
        None = 0,
        Code39 = 1,
        Code128 = 2,
        QrCode = 4,
        DataMatrix = 8,
        // 其他条码类型...
    }

    public enum CameraStatus {

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

    public enum CameraType {

        /// <summary>
        /// 工业相机
        /// </summary>
        IndustrialCamera,

        /// <summary>
        /// 全景相机
        /// </summary>
        PanoramicCamera,

        /// <summary>
        /// 3D相机
        /// </summary>
        ThreeDCamera,

        /// <summary>
        /// 智能相机
        /// </summary>
        SmartCamera
    }

    public enum ConnectionType {

        /// <summary>
        /// USB连接
        /// </summary>
        Usb,

        /// <summary>
        /// 网口连接
        /// </summary>
        Ethernet,

        /// <summary>
        /// 串口连接
        /// </summary>
        SerialPort,

        /// <summary>
        /// 蓝牙连接
        /// </summary>
        Bluetooth,

        /// <summary>
        /// Tcp连接
        /// </summary>
        Tcp
    }
}