namespace JayTom.Dws.Plugin.Contracts;

/// <summary>
/// 描述可由宿主发现和校验的插件清单。
/// </summary>
public sealed class PluginManifest {
    /// <summary>
    /// 获取插件的稳定唯一标识。
    /// </summary>
    public required string PluginKey { get; init; }

    /// <summary>
    /// 获取面向用户显示的插件名称。
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 获取符合语义化版本格式的插件版本。
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// 获取插件所需的最低宿主版本。
    /// </summary>
    public required string MinimumHostVersion { get; init; }

    /// <summary>
    /// 获取插件编译时使用的契约主版本。
    /// </summary>
    public required int ContractMajorVersion { get; init; }

    /// <summary>
    /// 获取插件入口类型的程序集限定名称。
    /// </summary>
    public required string EntryPoint { get; init; }

    /// <summary>
    /// 获取插件声明的能力名称集合。
    /// </summary>
    public IReadOnlyList<string> Capabilities { get; init; } = Array.Empty<string>();
}
