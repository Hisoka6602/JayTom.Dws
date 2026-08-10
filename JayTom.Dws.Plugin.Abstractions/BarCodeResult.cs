namespace JayTom.Dws.PluginInterface;

/// <summary>
/// 描述过滤插件处理后的包裹测量结果。
/// </summary>
public sealed class BarCodeResult {
    /// <summary>获取或设置条码。</summary>
    public string Barcode { get; set; } = string.Empty;
    /// <summary>获取或设置重量。</summary>
    public decimal Weight { get; set; }
    /// <summary>获取或设置长度。</summary>
    public decimal Length { get; set; }
    /// <summary>获取或设置宽度。</summary>
    public decimal Width { get; set; }
    /// <summary>获取或设置高度。</summary>
    public decimal Height { get; set; }
    /// <summary>获取或设置体积。</summary>
    public decimal Volume { get; set; }
    /// <summary>获取或设置条码图像。</summary>
    public PluginImage? Image { get; set; }
    /// <summary>获取或设置全景图像。</summary>
    public PluginImage? PanoramaImage { get; set; }
}
