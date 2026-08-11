using System.ComponentModel;

namespace JayTom.Dws.Abstractions.Devices;

/// <summary>
/// 灰度仪返回的包裹位置识别结果。
/// </summary>
public sealed class GrayscaleResult {
    /// <summary>获取或设置小车号。</summary>
    [Description("小车号")]
    public int CarNumber { get; set; }

    /// <summary>获取或设置附件框识别信息。</summary>
    [Description("附件框信息")]
    public BoxPackageInfo AttachmentRectangleBoxInfo { get; set; } = new();

    /// <summary>获取或设置主框识别信息。</summary>
    [Description("主框信息")]
    public List<BoxPackageInfo> MainRectangleBoxInfos { get; set; } = [];

    /// <summary>获取或设置联动小车数量。</summary>
    [Description("联动小车数量")]
    public int LinkedCarCount { get; set; }

    /// <summary>获取或设置包裹中心点。</summary>
    [Description("中心点")]
    public Point2D CenterPoint { get; set; }

    /// <summary>获取或设置结果产生时间。</summary>
    [Description("返回结果时间")]
    public DateTime ResultTime { get; set; }

    /// <summary>获取或设置结果是否超时。</summary>
    [Description("是否超时")]
    public bool IsTimeOut { get; set; }

    /// <summary>返回便于日志查看的中文识别结果。</summary>
    public override string ToString() {
        var boxes = string.Join(Environment.NewLine, MainRectangleBoxInfos);
        return $"小车号: {CarNumber}{Environment.NewLine}" +
               $"附件框信息:{Environment.NewLine}{AttachmentRectangleBoxInfo}{Environment.NewLine}" +
               $"主框信息:{Environment.NewLine}{boxes}{Environment.NewLine}" +
               $"联动小车数量: {LinkedCarCount}{Environment.NewLine}" +
               $"中心点: {CenterPoint}";
    }
}
