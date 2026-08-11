using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Camera.BarCodeReader {

    /// <summary>
    /// 读码器
    /// </summary>
    public interface IBarCodeReader : IDisposable {

        /// <summary>
        /// 读一帧图片
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<BarcodeResult> ReadFromFrameAsync(Bitmap bitmap, CancellationToken token = default);

        /// <summary>
        /// 回调条码事件
        /// </summary>
        event EventHandler<BarcodeResult> BarcodeRead;

        /// <summary>
        /// 提交实时图片到队列
        /// </summary>
        /// <param name="bitmap">要提交的图片</param>
        void EnqueueFrame(Bitmap bitmap);

        /// <summary>
        /// 设置读码参数
        /// </summary>
        /// <param name="parameters">读码参数</param>
        Task<KeyValuePair<bool, string>> ApplySettingsAsync(
            BarcodeReaderSettings settings,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 异常回调事件
        /// </summary>
        event EventHandler<Exception> ExceptionOccurred;

        /// <summary>
        /// 初始化读码器
        /// </summary>
        Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取一个值，该值指示读码器是否已初始化
        /// </summary>
        bool IsInitialized { get; }
    }

    /// <summary>
    /// 读码结果
    /// </summary>
    public class BarcodeResult {

        /// <summary>
        /// 图片
        /// </summary>
        public Bitmap? Image { get; set; }

        /// <summary>
        /// 条码集合
        /// </summary>
        public List<BarcodeInfo>? BarCodes { get; set; }

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime ScanTime { get; set; }

        /// <summary>
        /// 识别耗时
        /// </summary>
        public long RecognitionDurationMilliseconds { get; set; }
    }

    public class BarcodeInfo {

        /// <summary>
        /// 条码
        /// </summary>
        public string? Barcode { get; set; }

        /// <summary>
        /// 条码区域
        /// </summary>
        public List<Point>? BarcodeRegion { get; set; }

        /// <summary>
        /// 条码类型
        /// </summary>
        public string? BarcodeType { get; set; }
    }

    /// <summary>
    /// 本地化模式
    /// </summary>
    public enum LocalizationMode {

        /// <summary>
        /// 默认
        /// </summary>
        Default = 0,

        /// <summary>
        /// 连通块
        /// </summary>
        ConnectedBlocks = 1,

        /// <summary>
        /// 统计
        /// </summary>
        Statistics = 2,

        /// <summary>
        /// 线条
        /// </summary>
        Lines = 3,

        /// <summary>
        /// 直接扫描
        /// </summary>
        ScanDirectly = 4,

        /// <summary>
        /// 连通块 + 直接扫描
        /// </summary>
        ConnectedBlocksAndScanDirectly = 5
    }

    /// <summary>
    /// 灰度转换模式
    /// </summary>
    public enum GrayscaleTransformationMode {

        /// <summary>
        /// 原图
        /// </summary>
        Original = 0,

        /// <summary>
        /// 反色
        /// </summary>
        Inverted = 1,

        /// <summary>
        /// 原图+反色
        /// </summary>
        OriginalAndInverted = 2
    }

    /// <summary>
    /// 图像预处理模式
    /// </summary>
    public enum ImagePreprocessingMode {

        /// <summary>
        /// 通用
        /// </summary>
        General = 0,

        /// <summary>
        /// 灰度均衡化
        /// </summary>
        GrayEqualization = 1,

        /// <summary>
        /// 灰度平滑
        /// </summary>
        GraySmoothing = 2,

        /// <summary>
        /// 锐化和平滑
        /// </summary>
        SharpeningAndSmoothing = 3
    }

    /// <summary>
    /// 扫码模式
    /// </summary>
    public enum ScanMode {

        /// <summary>
        /// 速度
        /// </summary>
        Speed,

        /// <summary>
        /// 平衡
        /// </summary>
        Balance,

        /// <summary>
        /// 覆盖
        /// </summary>
        Coverage,

        /// <summary>
        /// 自定义
        /// </summary>

        Custom
    }

    /// <summary>
    /// 读码参数
    /// </summary>
    public enum BarcodeReaderParameter {

        /// <summary>
        /// 条码类型
        /// </summary>
        EnumBarcodeFormat,

        /// <summary>
        /// 条码类型2
        /// </summary>
        EnumBarcodeFormat2,

        /// <summary>
        /// 本地化模式
        /// </summary>
        LocalizationMode,

        /// <summary>
        /// 去模糊级别(0-9)
        /// </summary>
        DeblurLevel,

        /// <summary>
        /// 期望的条形码数量
        /// </summary>
        ExpectedBarcodesCount,

        /// <summary>
        /// 缩放阈值
        /// </summary>
        ScaleDownThreshold,

        /// <summary>
        /// 是否使用文本过滤模式
        /// </summary>
        IsUseTextFilterMode,

        /// <summary>
        /// 是否使用区域预检测模式
        /// </summary>
        IsUseRegionPredetectionMode,

        /// <summary>
        /// 灰度转换模式
        /// </summary>
        GrayscaleTransformationMode,

        /// <summary>
        /// 图像预处理模式
        /// </summary>
        ImagePreprocessingMode,

        /// <summary>
        /// 最小结果置信度(0-9,乘以10)
        /// </summary>
        MinResultConfidence,

        /// <summary>
        /// 纹理检测敏感度(0-9)
        /// </summary>
        TextureDetectionSensitivity,

        /// <summary>
        /// 二值化块大小(0-999)
        /// </summary>
        BinarizationBlockSize,

        /// <summary>
        /// 识别模式
        /// </summary>
        RecognitionMode,

        /// <summary>
        /// 跳过的帧率
        /// </summary>
        RecognitionSkipFrames,

        /// <summary>
        /// 图片缩放百分比
        /// </summary>
        ScalePercentage,
    }
}
