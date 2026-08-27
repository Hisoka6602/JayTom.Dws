namespace JayTom.Dws.Plugin.Runtime;

/// <summary>汇总一次插件目录发现产生的可用句柄与隔离诊断。</summary>
public sealed record PluginDiscoveryResult
{
    /// <summary>获取成功加载且由调用方负责释放的插件句柄。</summary>
    public required IReadOnlyList<PluginHandle> Plugins { get; init; }

    /// <summary>获取未阻断其他插件加载的失败诊断。</summary>
    public required IReadOnlyList<PluginLoadDiagnostic> Diagnostics { get; init; }
}
