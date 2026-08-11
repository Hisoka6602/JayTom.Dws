namespace JayTom.Dws.Camera.BarCodeReader;

/// <summary>
/// 表示相机层对上层公开的条码格式能力，避免泄漏具体厂商枚举。
/// </summary>
[Flags]
public enum SupportedBarcodeFormat {
    /// <summary>
    /// 未指定条码格式。
    /// </summary>
    None = 0,

    /// <summary>
    /// 二维码格式。
    /// </summary>
    QrCode = 1,

    /// <summary>
    /// 微型二维码格式。
    /// </summary>
    MicroQr = 2,

    /// <summary>
    /// Code 128 条码格式。
    /// </summary>
    Code128 = 4,

    /// <summary>
    /// Code 39 条码格式。
    /// </summary>
    Code39 = 8,

    /// <summary>
    /// Code 93 条码格式。
    /// </summary>
    Code93 = 16,

    /// <summary>
    /// Codabar 条码格式。
    /// </summary>
    Codabar = 32,

    /// <summary>
    /// EAN-13 条码格式。
    /// </summary>
    Ean13 = 64,

    /// <summary>
    /// EAN-8 条码格式。
    /// </summary>
    Ean8 = 128
}
