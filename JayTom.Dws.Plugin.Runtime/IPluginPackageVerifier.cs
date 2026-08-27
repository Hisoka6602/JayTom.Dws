// DWS-COHESIVE-CONTRACTS: 插件包验证端口与结果共同定义信任判定。
using JayTom.Dws.Plugin.Contracts;

namespace JayTom.Dws.Plugin.Runtime;

/// <summary>在加载程序集前验证插件来源、完整性和权限。</summary>
public interface IPluginPackageVerifier {
    /// <summary>验证插件包。</summary>
    ValueTask<PluginPackageVerificationResult> VerifyAsync(
        PluginManifest manifest,
        string assemblyPath,
        CancellationToken cancellationToken);
}

/// <summary>表示插件包信任校验结果。</summary>
public sealed record PluginPackageVerificationResult(bool IsTrusted, string Message) {
    /// <summary>创建信任结果。</summary>
    public static PluginPackageVerificationResult Trusted() => new(true, string.Empty);

    /// <summary>创建拒绝结果。</summary>
    public static PluginPackageVerificationResult Rejected(string message) => new(false, message);
}
