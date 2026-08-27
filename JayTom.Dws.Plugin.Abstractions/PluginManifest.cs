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

    /// <summary>获取插件请求的最小权限集合。</summary>
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();

    /// <summary>获取签名公钥在宿主信任库中的稳定标识。</summary>
    public string SigningKeyId { get; init; } = string.Empty;

    /// <summary>获取入口程序集的 SHA-256 十六进制摘要。</summary>
    public string AssemblySha256 { get; init; } = string.Empty;

    /// <summary>获取清单与程序集摘要的 RSA-PSS/SHA-256 签名。</summary>
    public string Signature { get; init; } = string.Empty;
}
