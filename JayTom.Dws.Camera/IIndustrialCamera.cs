using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;

namespace JayTom.Dws.Camera {

    /// <summary>
    /// 工业相机接口
    /// </summary>
    public interface IIndustrialCamera : ICamera {

        /// <summary>
        /// 条码边框大小
        /// </summary>
        public int BarcodeBorderSize { get; set; }

        /// <summary>
        /// 边框颜色
        /// </summary>
        public System.Drawing.Color BarcodeBorderColor { get; set; }

        /// <summary>
        /// 是否显示边框
        /// </summary>
        public bool IsShowBarcodeBorder { get; set; }

        /// <summary>
        /// 是否开启实时图像
        /// </summary>
        public bool IsRealtimeImageEnabled { get; set; }

        /// <summary>
        /// 读取到条码事件
        /// </summary>
        event EventHandler<BarcodeReadEventArgs> BarcodeRead;

        /// <summary>
        /// 实时图像事件
        /// </summary>
        event EventHandler<RealtimeImageEventArgs> RealtimeImage;

        //软触发方法
    }

    /// <summary>
    /// 条码读取事件参数
    /// </summary>
    public class BarcodeReadEventArgs : EventArgs {

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
        /// 帧率
        /// </summary>
        public double FrameRate { get; set; }

        /// <summary>
        /// 区域坐标
        /// </summary>
        public List<Point>? AreaCoords { get; set; }

        /// <summary>
        /// 缩略图
        /// </summary>
        public Bitmap? ThumbImage { get; set; }

        /// <summary>
        /// 条码时间戳
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;
    }

    /// <summary>
    /// 实时图像事件参数
    /// </summary>
    public class RealtimeImageEventArgs : EventArgs {

        /// <summary>
        /// 图像帧时间戳
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 图片
        /// </summary>
        public Bitmap? Image { get; set; }

        /// <summary>
        /// 缩略图
        /// </summary>
        public Bitmap? ThumbImage { get; set; }
    }
}