namespace JayTom.Dws.Camera;

/// <summary>描述扫码结果的过滤规则。</summary>
public class ScanCodeFilterParams {
    /// <summary>获取或设置扫码时间间隔毫秒数。</summary>
    public int ScanInterval { get; set; } = 500;

    /// <summary>获取或设置基础正则表达式。</summary>
    public string RegularExpression { get; set; } = string.Empty;

    /// <summary>获取或设置重复条码过滤数量。</summary>
    public int DuplicateBarcodeFilterCount { get; set; }

    /// <summary>获取或设置过滤后的替代输出。</summary>
    public string FilterOutContent { get; set; } = string.Empty;

    /// <summary>获取或设置过滤模式。</summary>
    public BarCodeFilterMode BarCodeFilterMode { get; set; } = BarCodeFilterMode.None;

    /// <summary>获取或设置自定义正则表达式列表。</summary>
    public List<string> CustomRegularExpressionItems { get; set; } = [];

    /// <summary>获取或设置是否启用自定义正则替换。</summary>
    public bool IsUseCustomRegexReplacement { get; set; }

    /// <summary>获取或设置是否过滤指定条码类型。</summary>
    public bool IsUseFilteredBarcodeTypes { get; set; }

    /// <summary>获取或设置自定义正则替换项。</summary>
    public List<CustomRegexReplacementItemInfo> CustomRegexReplacementItems { get; set; } = [];
}
