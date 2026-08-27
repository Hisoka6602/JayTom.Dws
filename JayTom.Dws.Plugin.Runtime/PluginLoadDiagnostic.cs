namespace JayTom.Dws.Plugin.Runtime;

/// <summary>提供不持有插件异常对象的可序列化加载诊断。</summary>
public sealed record PluginLoadDiagnostic
{
    /// <summary>获取产生诊断的清单绝对路径。</summary>
    public required string ManifestPath { get; init; }

    /// <summary>获取可用时的插件稳定标识。</summary>
    public string? PluginKey { get; init; }

    /// <summary>获取稳定失败分类。</summary>
    public required PluginLoadStatus Status { get; init; }

    /// <summary>获取不包含凭据和堆栈的诊断说明。</summary>
    public required string Message { get; init; }

    /// <summary>获取可用时的异常类型名。</summary>
    public string? ExceptionType { get; init; }
}
