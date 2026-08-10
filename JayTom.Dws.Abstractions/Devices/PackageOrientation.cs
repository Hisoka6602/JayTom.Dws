using System.ComponentModel;

namespace JayTom.Dws.Abstractions.Devices;

/// <summary>
/// 包裹相对检测框中心的偏向。
/// </summary>
public enum PackageOrientation {
    /// <summary>偏左。</summary>
    [Description("偏左")]
    Left,

    /// <summary>偏右。</summary>
    [Description("偏右")]
    Right,

    /// <summary>居中。</summary>
    [Description("居中")]
    Center
}
