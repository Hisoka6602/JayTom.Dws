namespace JayTom.Dws.Plugin.Contracts;

/// <summary>
/// 封装一次插件兼容性检查的状态与可诊断说明。
/// </summary>
public sealed record PluginCompatibilityResult {
    /// <summary>
    /// 获取兼容性状态。
    /// </summary>
    public required PluginCompatibilityStatus Status { get; init; }

    /// <summary>
    /// 获取供日志和界面展示的检查说明。
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// 获取插件是否可以由当前宿主加载。
    /// </summary>
    public bool IsCompatible => Status == PluginCompatibilityStatus.Compatible;
}
