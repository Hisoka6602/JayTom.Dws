namespace JayTom.Dws.Plugin.Runtime;

/// <summary>定义插件签名信任根、吊销列表和宿主权限白名单。</summary>
public sealed record PluginTrustOptions {
    /// <summary>获取信任公钥目录。</summary>
    public required string TrustDirectory { get; init; }

    /// <summary>获取被吊销的签名密钥标识。</summary>
    public IReadOnlySet<string> RevokedKeyIds { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>获取宿主允许授予插件的权限。</summary>
    public IReadOnlySet<string> AllowedPermissions { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>获取允许读取的单个程序集最大长度。</summary>
    public long MaximumAssemblyBytes { get; init; } = 512L * 1024 * 1024;
}
