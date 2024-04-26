using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Camera {

    /// <summary>
    /// 体积相机
    /// </summary>
    public interface IVolumeCamera : ICamera {

        /// <summary>
        /// 触发模式
        /// </summary>
        public MeasurementTriggerMode MeasurementTriggerMode { get; set; }

        /// <summary>
        /// 当相机捕获到体积信息时触发的事件
        /// </summary>
        event EventHandler<VolumeCapturedEventArgs> VolumeCaptured;

        /// <summary>
        /// 触发一次体积测量
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="barcodeTimestamp"></param>
        /// <param name="delay"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        Task TriggerMeasurementPhotoAsync(string barcode, long barcodeTimestamp, int delay, CancellationToken cancellation = default);
    }

    public class VolumeCapturedEventArgs : EventArgs {

        /// <summary>
        /// 获取或设置物体的长度。
        /// </summary>
        public double Length { get; set; }

        /// <summary>
        /// 获取或设置物体的宽度。
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// 获取或设置物体的高度。
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 获取或设置捕获的原始图像。
        /// </summary>
        public Bitmap? Image { get; set; }

        /// <summary>
        /// 获取或设置物体的体积。
        /// </summary>
        public double Volume { get; set; }

        /// <summary>
        /// 获取或设置捕获时间戳。
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 获取或设置缩略图像。
        /// </summary>
        public Bitmap? Thumbnail { get; set; }

        /// <summary>
        /// 获取或设置物体区域的坐标数组。
        /// </summary>
        public System.Drawing.Point[]? AreaCoords { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 触发模式
        /// </summary>
        public MeasurementTriggerMode MeasurementTriggerMode { get; set; } = MeasurementTriggerMode.Continuous;
    }

    public enum MeasurementTriggerMode {

        /// <summary>
        /// 连续触发
        /// </summary>
        Continuous,

        /// <summary>
        /// 单次触发
        /// </summary>
        Single
    }
}