using System.ComponentModel;
using JayTom.Dws.Camera.Attributes.CameraAttributes;

namespace JayTom.Dws.Camera;

/// <summary>描述相机可承担的业务用途。</summary>
[Flags]
public enum CameraBindingType {
    /// <summary>扫码相机。</summary>
    [CameraFontIcon("\xe9f5"), CameraBackgroundColor("#4169E1"), Description("扫码相机")]
    ScannerCamera = 1 << 0,

    /// <summary>全景相机。</summary>
    [CameraFontIcon("\xe605"), CameraBackgroundColor("#8A2BE2"), Description("全景相机")]
    PanoramaCamera = 1 << 1,

    /// <summary>体积相机。</summary>
    [CameraFontIcon("\xea1a"), CameraBackgroundColor("#1E90FF"), Description("体积相机")]
    VolumeCamera = 1 << 2,

    /// <summary>OCR 相机。</summary>
    [CameraFontIcon("\xe7a3"), CameraBackgroundColor("#FF8C00"), Description("Ocr识别")]
    OcrCamera = 1 << 3
}
