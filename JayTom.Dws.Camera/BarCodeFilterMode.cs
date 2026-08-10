namespace JayTom.Dws.Camera;

/// <summary>描述条码过滤策略。</summary>
public enum BarCodeFilterMode {
    /// <summary>不过滤。</summary>
    None = 0,
    /// <summary>使用基础规则过滤。</summary>
    BasicFilter = 1,
    /// <summary>使用自定义正则过滤。</summary>
    CustomRegexFilter = 2
}
