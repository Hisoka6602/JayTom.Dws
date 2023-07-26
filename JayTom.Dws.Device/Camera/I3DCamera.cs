using System.Drawing;

namespace JayTom.Dws.Device.Camera {

    public interface I3DCamera : ICamera {

        /// <summary>
        /// 边框大小
        /// </summary>
        int DetectionBorderSize { get; set; }

        /// <summary>
        /// 边框颜色
        /// </summary>
        Color DetectionBorderColor { get; set; }

        /// <summary>
        /// 是否显示边框
        /// </summary>
        bool IsShowDetectionBorder { get; set; }

        /// <summary>
        /// 捕捉到体积事件
        /// </summary>
        event EventHandler<VolumeCapturedEventArgs> VolumeCapturedEvent;

        /// <summary>
        /// 实时映射图事件
        /// </summary>
        event EventHandler<Bitmap> LiveMappingEvent;

        /// <summary>
        /// 设备警告事件
        /// </summary>
        event EventHandler<string> DeviceWarning;

        /// <summary>
        /// 物品超出边缘
        /// </summary>
        public event EventHandler<ItemOutOfBoundsEventArgs>? ItemOutOfBounds;

        /// <summary>
        /// 画面变动未检测到物品
        /// </summary>
        public event EventHandler<EventArgs>? ItemNotDetected;
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
        public Point[]? AreaCoords { get; set; }
    }

    public class ItemOutOfBoundsEventArgs : EventArgs {
        public OutOfBoundsDirection Direction { get; set; } // 超出边缘的方位
    }

    public enum OutOfBoundsDirection {
        Up,
        Down,
        Left,
        Right
    }
}