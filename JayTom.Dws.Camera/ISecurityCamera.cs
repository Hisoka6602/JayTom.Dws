using System;
using System.Linq;
using System.Text;
using System.Timers;
using System.Drawing;
using JayTom.Dws.Ocr;
using System.ComponentModel;
using JayTom.Dws.Camera.Nvr;
using System.Threading.Tasks;
using System.Collections.Generic;
using Timer = System.Timers.Timer;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Camera {

    public interface ISecurityCamera : ICamera {

        /// <summary>
        /// 相机连接参数
        /// </summary>
        string CameraConnectionParameters { get; set; }

        /// <summary>
        /// 保存流
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> SaveStream(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// 缩放
        /// </summary>
        /// <param name="zoomFactor"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> Zoom(double zoomFactor, CancellationToken cancellationToken = default);

        /// <summary>
        /// 云台控制
        /// </summary>
        /// <param name="panAngle"></param>
        /// <param name="tiltAngle"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> ControlPtz(double panAngle, double tiltAngle, CancellationToken cancellationToken = default);

        /// <summary>
        /// 设置步长
        /// </summary>
        /// <param name="stepSize"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> SetStepSize(int stepSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// 设置焦距
        /// </summary>
        /// <param name="focalLength"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> SetFocalLength(double focalLength, CancellationToken cancellationToken = default);

        /// <summary>
        /// 设置光圈
        /// </summary>
        /// <param name="aperture"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> SetAperture(double aperture, CancellationToken cancellationToken = default);

        /// <summary>
        /// 远程回放实时画面事件
        /// </summary>
        public event EventHandler<RemotePlaybackEventArgs> RemotePlaybackRealtimeImage;

        /// <summary>
        /// 开始远程回放
        /// </summary>
        /// <param name="playbackSpeed"></param>
        public void StartRemotePlayback(int playbackSpeed);

        /// <summary>
        /// 停止远程回放
        /// </summary>
        public void StopRemotePlayback();

        /// <summary>
        /// 暂停远程回放
        /// </summary>
        public void PauseRemotePlayback();

        /// <summary>
        /// Ocr对象
        /// </summary>
        public IOcr? Ocr { get; set; }

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
        /// 读取到条码事件
        /// </summary>
        event EventHandler<BarcodeReadEventArgs> BarcodeRead;

        /// <summary>
        /// 当OCR识别到内容时触发的事件
        /// </summary>
        event EventHandler<OcrResult> OcrContentRecognized;

        /// <summary>
        /// 过滤的条码返回事件
        /// </summary>
        event EventHandler<BarcodeReadEventArgs> FilteredBarcodeReturned;

        /// <summary>
        /// 设置扫码过滤参数
        /// </summary>
        /// <param name="params"></param>
        /// <returns></returns>
        void SetScanCodeFilterParams([NotNull] ScanCodeFilterParams @params);
    }

    public class RealPreviewEventArgs {
    }

    /// <summary>
    /// 安防相机连接参数
    /// </summary>
    public class SecurityCameraConnectionParameters {

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 播放通道
        /// </summary>
        public int PlayChannelId { get; set; } = 0;
    }

    public class CameraImageMessageInfo {
        public string Barcode { get; set; } = string.Empty;

        public long BarcodeTimestamp { get; set; }
    }

    /// <summary>
    /// 配置水印属性的类，包括位置、最大水印数量、水印持续时间、前景色和背景色。
    /// </summary>
    public class SecurityCameraWatermarkConfig {

        /// <summary>
        /// 水印的位置枚举，表示水印可以放置在屏幕的四个角落。
        /// </summary>
        public enum WatermarkPosition {

            /// <summary>
            /// 水印在左上角。
            /// </summary>
            [Description("左上角")]
            TopLeft,

            /// <summary>
            /// 水印在右上角。
            /// </summary>
            [Description("右上角")]
            TopRight,

            /// <summary>
            /// 水印在左下角。
            /// </summary>
            [Description("左下角")]
            BottomLeft,

            /// <summary>
            /// 水印在右下角。
            /// </summary>
            [Description("右下角")]
            BottomRight
        }

        /// <summary>
        /// 获取或设置水印的位置。
        /// </summary>
        public WatermarkPosition Position { get; set; } = WatermarkPosition.TopLeft;

        /// <summary>
        /// 获取或设置最大水印数量。
        /// 这表示可以同时显示的水印的最大数量。
        /// </summary>
        public int MaxWatermarks { get; set; } = 4;

        /// <summary>
        /// 获取或设置水印的持续时间。
        /// 这表示每个水印从显示到消失的时间长度。
        /// </summary>
        public int Duration { get; set; } = 3;

        /// <summary>
        /// 获取或设置水印的前景色。
        /// 颜色值使用字符串表示，例如 "#FFFFFF" 表示白色。
        /// </summary>
        public Color ForegroundColor { get; set; } = Color.Red;

        /// <summary>
        /// 获取或设置水印的背景色。
        /// 颜色值使用字符串表示，例如 "#000000" 表示黑色。
        /// </summary>
        public Color BackgroundColor { get; set; } = Color.White;
    }

    public class HistoricalWatermark : SecurityCameraWatermarkConfig, IDisposable {

        /// <summary>
        /// 获取或设置水印的唯一标识符。
        /// </summary>
        public string SerialNo { get; set; }

        /// <summary>
        /// 获取或设置频道的标识符，用于关联特定频道。
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// 获取或设置水印的内容，如文本或图像描述。
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 获取或设置水印的添加时间。
        /// </summary>
        public DateTime AddedTime { get; set; }

        /// <summary>
        /// 登录ID
        /// </summary>
        public nint LoginId { get; set; }

        /// <summary>
        /// 获取或设置自动删除水印的计时器。
        /// 当时间到达时自动触发删除。
        /// </summary>
        public Timer? AutoDeleteTimer { get; private set; }

        /// <summary>
        /// 用户自定义的处理方法，当自动删除计时器到期时调用。
        /// </summary>
        public Action<HistoricalWatermark>? OnAutoDelete { get; set; }

        public HistoricalWatermark(
            string serialNo,
            nint loginId,
            int channelId,
            string content,
            DateTime addedTime,
            double autoDeleteInterval,
            Action<HistoricalWatermark>? onAutoDelete = null) {
            SerialNo = serialNo;
            LoginId = loginId;
            ChannelId = channelId;
            Content = content;
            AddedTime = addedTime;
            OnAutoDelete = onAutoDelete;

            // 初始化自动删除计时器
            AutoDeleteTimer = new Timer(autoDeleteInterval);
            AutoDeleteTimer.Elapsed += OnAutoDeleteTimerElapsed;
            AutoDeleteTimer.AutoReset = false; // 只触发一次
            AutoDeleteTimer.Start();
        }

        public void Dispose() {
            AutoDeleteTimer?.Stop();
            AutoDeleteTimer?.Dispose();
        }

        /// <summary>
        /// 当自动删除计时器到期时，执行的自定义方法。
        /// </summary>
        /// <param name="sender">事件的来源对象。</param>
        /// <param name="e">事件参数。</param>
        private void OnAutoDeleteTimerElapsed(object? sender, ElapsedEventArgs e) {
            // 如果用户定义了自定义处理方法，则调用它
            OnAutoDelete?.Invoke(this);

            this.Dispose();
        }
    }
}