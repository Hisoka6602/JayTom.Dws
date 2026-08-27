using System.Drawing;

namespace JayTom.Dws.Camera;

/// <summary>
/// 描述 USB 相机可选画面参数；空值表示沿用设备自动配置。
/// </summary>
public sealed class UsbCameraSettings
{
    /// <summary>获取或初始化分辨率。</summary>
    public Size? Resolution { get; init; }

    /// <summary>获取或初始化曝光度。</summary>
    public int? Exposure { get; init; }

    /// <summary>获取或初始化亮度。</summary>
    public int? Brightness { get; init; }

    /// <summary>获取或初始化对比度。</summary>
    public int? Contrast { get; init; }

    /// <summary>获取或初始化色调。</summary>
    public int? Hue { get; init; }

    /// <summary>获取或初始化饱和度。</summary>
    public int? Saturation { get; init; }

    /// <summary>获取或初始化锐度。</summary>
    public int? Sharpness { get; init; }

    /// <summary>获取或初始化伽马值。</summary>
    public int? Gamma { get; init; }

    /// <summary>获取或初始化白平衡。</summary>
    public int? WhiteBalance { get; init; }

    /// <summary>获取或初始化背光补偿。</summary>
    public int? BacklightCompensation { get; init; }

    /// <summary>获取或初始化增益。</summary>
    public int? Gain { get; init; }

    /// <summary>获取或初始化变焦。</summary>
    public int? Zoom { get; init; }

    /// <summary>获取或初始化对焦。</summary>
    public int? Focus { get; init; }

    /// <summary>获取或初始化光圈。</summary>
    public int? Iris { get; init; }

    /// <summary>获取或初始化水平旋转。</summary>
    public int? Pan { get; init; }

    /// <summary>获取或初始化垂直旋转。</summary>
    public int? Tilt { get; init; }

    /// <summary>获取或初始化翻转。</summary>
    public int? Roll { get; init; }
}
