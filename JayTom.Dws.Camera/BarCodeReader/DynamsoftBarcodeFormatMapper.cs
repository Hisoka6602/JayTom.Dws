using Dynamsoft;

namespace JayTom.Dws.Camera.BarCodeReader;

/// <summary>
/// 将公开条码格式转换为 Dynamsoft SDK 的内部格式。
/// </summary>
internal static class DynamsoftBarcodeFormatMapper {
    /// <summary>
    /// 转换一组可组合的公开条码格式。
    /// </summary>
    /// <param name="format">公开条码格式。</param>
    /// <returns>Dynamsoft SDK 使用的条码格式。</returns>
    internal static EnumBarcodeFormat Map(SupportedBarcodeFormat format) {
        var result = (EnumBarcodeFormat)0;
        AddIfSelected(format, SupportedBarcodeFormat.QrCode, EnumBarcodeFormat.BF_QR_CODE, ref result);
        AddIfSelected(format, SupportedBarcodeFormat.MicroQr, EnumBarcodeFormat.BF_MICRO_QR, ref result);
        AddIfSelected(format, SupportedBarcodeFormat.Code128, EnumBarcodeFormat.BF_CODE_128, ref result);
        AddIfSelected(format, SupportedBarcodeFormat.Code39, EnumBarcodeFormat.BF_CODE_39, ref result);
        AddIfSelected(format, SupportedBarcodeFormat.Code93, EnumBarcodeFormat.BF_CODE_93, ref result);
        AddIfSelected(format, SupportedBarcodeFormat.Codabar, EnumBarcodeFormat.BF_CODABAR, ref result);
        AddIfSelected(format, SupportedBarcodeFormat.Ean13, EnumBarcodeFormat.BF_EAN_13, ref result);
        AddIfSelected(format, SupportedBarcodeFormat.Ean8, EnumBarcodeFormat.BF_EAN_8, ref result);
        return result;
    }

    /// <summary>
    /// 在公开格式包含指定值时合并对应的 SDK 格式。
    /// </summary>
    /// <param name="source">公开格式集合。</param>
    /// <param name="candidate">待检查的公开格式。</param>
    /// <param name="mapped">对应的 SDK 格式。</param>
    /// <param name="result">累计的 SDK 格式。</param>
    private static void AddIfSelected(
        SupportedBarcodeFormat source,
        SupportedBarcodeFormat candidate,
        EnumBarcodeFormat mapped,
        ref EnumBarcodeFormat result) {
        if ((source & candidate) == candidate) {
            result |= mapped;
        }
    }
}
