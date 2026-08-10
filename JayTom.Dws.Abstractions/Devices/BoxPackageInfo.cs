namespace JayTom.Dws.Abstractions.Devices;

/// <summary>
/// 单个检测框内的包裹位置数据。
/// </summary>
public sealed class BoxPackageInfo {
    /// <summary>获取或设置检测框内是否存在包裹。</summary>
    public bool IsPackagePresent { get; set; }

    /// <summary>获取或设置包裹区域坐标。</summary>
    public Coordinates PackageRegionCoordinates { get; set; }

    /// <summary>获取或设置包裹偏向。</summary>
    public PackageOrientation PackageOrientation { get; set; } = PackageOrientation.Center;

    /// <summary>获取或设置偏向距离。</summary>
    public int OrientationValue { get; set; }

    /// <summary>获取或设置偏向百分比。</summary>
    public decimal OffsetPercentage { get; set; }

    /// <summary>获取或设置包裹占比。</summary>
    public decimal PackageRatio { get; set; }

    /// <summary>返回便于日志查看的中文检测框信息。</summary>
    public override string ToString() =>
        $"是否存在包裹: {IsPackagePresent}, " +
        $"包裹区域坐标: {PackageRegionCoordinates}, " +
        $"包裹偏向: {PackageOrientation}, " +
        $"偏向值: {OrientationValue}, " +
        $"偏向百分比: {OffsetPercentage:P2}, " +
        $"包裹占比: {PackageRatio:P2}";
}
