using Dynamsoft.DBR;

namespace JayTom.Dws.Camera.BarCodeReader;

/// <summary>定义类型安全的读码器运行参数。</summary>
public sealed record BarcodeReaderSettings {
    /// <summary>获取要识别的条码格式。</summary>
    public SupportedBarcodeFormat BarcodeFormats { get; init; } = SupportedBarcodeFormat.None;

    /// <summary>获取识别模式。</summary>
    public ScanMode RecognitionMode { get; init; } = ScanMode.Speed;

    /// <summary>获取期望识别的条码数量。</summary>
    public int ExpectedBarcodesCount { get; init; } = 1;

    /// <summary>获取去模糊级别。</summary>
    public int DeblurLevel { get; init; } = 3;

    /// <summary>获取本地化模式。</summary>
    public LocalizationMode LocalizationMode { get; init; }

    /// <summary>获取是否使用文本过滤。</summary>
    public bool UseTextFilter { get; init; }

    /// <summary>获取是否使用区域预检测。</summary>
    public bool UseRegionPredetection { get; init; }

    /// <summary>获取缩小处理阈值。</summary>
    public int ScaleDownThreshold { get; init; }

    /// <summary>获取灰度转换模式。</summary>
    public GrayscaleTransformationMode GrayscaleTransformationMode { get; init; }

    /// <summary>获取图像预处理模式。</summary>
    public ImagePreprocessingMode ImagePreprocessingMode { get; init; }

    /// <summary>获取最低识别置信度。</summary>
    public int MinimumResultConfidence { get; init; }

    /// <summary>获取纹理检测灵敏度。</summary>
    public int TextureDetectionSensitivity { get; init; }

    /// <summary>获取二值化块大小。</summary>
    public int BinarizationBlockSize { get; init; }

    /// <summary>获取识别跳帧数量。</summary>
    public int RecognitionSkipFrames { get; init; }

    /// <summary>获取输入图像缩放百分比。</summary>
    public int ScalePercentage { get; init; }

    /// <summary>将类型安全参数转换为 Dynamsoft 适配器内部参数。</summary>
    internal Dictionary<BarcodeReaderParameter, object> ToAdapterParameters() => new() {
        [BarcodeReaderParameter.EnumBarcodeFormat] = BarcodeFormats,
        [BarcodeReaderParameter.RecognitionMode] = RecognitionMode,
        [BarcodeReaderParameter.ExpectedBarcodesCount] = ExpectedBarcodesCount,
        [BarcodeReaderParameter.DeblurLevel] = DeblurLevel,
        [BarcodeReaderParameter.LocalizationMode] = LocalizationMode,
        [BarcodeReaderParameter.IsUseTextFilterMode] = UseTextFilter,
        [BarcodeReaderParameter.IsUseRegionPredetectionMode] = UseRegionPredetection,
        [BarcodeReaderParameter.ScaleDownThreshold] = ScaleDownThreshold,
        [BarcodeReaderParameter.GrayscaleTransformationMode] = GrayscaleTransformationMode,
        [BarcodeReaderParameter.ImagePreprocessingMode] = ImagePreprocessingMode,
        [BarcodeReaderParameter.MinResultConfidence] = MinimumResultConfidence,
        [BarcodeReaderParameter.TextureDetectionSensitivity] = TextureDetectionSensitivity,
        [BarcodeReaderParameter.BinarizationBlockSize] = BinarizationBlockSize,
        [BarcodeReaderParameter.RecognitionSkipFrames] = RecognitionSkipFrames,
        [BarcodeReaderParameter.ScalePercentage] = ScalePercentage
    };
}
