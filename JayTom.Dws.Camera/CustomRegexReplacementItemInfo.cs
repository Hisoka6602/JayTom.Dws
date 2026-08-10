namespace JayTom.Dws.Camera;

/// <summary>描述一条正则替换规则。</summary>
public class CustomRegexReplacementItemInfo {
    /// <summary>获取或设置正则表达式。</summary>
    public string RegexPattern { get; set; } = string.Empty;

    /// <summary>获取或设置替换内容。</summary>
    public string ReplaceContent { get; set; } = string.Empty;
}
