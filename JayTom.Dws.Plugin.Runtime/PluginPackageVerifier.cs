// DWS-COHESIVE-CONTRACTS: 生产验证器与显式开发验证器共同呈现安全模式差异。
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JayTom.Dws.Plugin.Contracts;

namespace JayTom.Dws.Plugin.Runtime;

/// <summary>使用外置信任根验证插件程序集摘要、清单签名和权限。</summary>
public sealed class PluginPackageVerifier : IPluginPackageVerifier {
    private readonly PluginTrustOptions _options;

    /// <summary>创建生产插件包验证器。</summary>
    public PluginPackageVerifier(PluginTrustOptions options) => _options = options;

    /// <inheritdoc />
    public async ValueTask<PluginPackageVerificationResult> VerifyAsync(
        PluginManifest manifest,
        string assemblyPath,
        CancellationToken cancellationToken) {
        if (!IsValidKeyId(manifest.SigningKeyId)) {
            return PluginPackageVerificationResult.Rejected("插件签名密钥标识格式无效。");
        }

        if (string.IsNullOrWhiteSpace(manifest.SigningKeyId) ||
            string.IsNullOrWhiteSpace(manifest.AssemblySha256) ||
            string.IsNullOrWhiteSpace(manifest.Signature)) {
            return PluginPackageVerificationResult.Rejected("插件缺少签名元数据。");
        }

        if (_options.RevokedKeyIds.Contains(manifest.SigningKeyId)) {
            return PluginPackageVerificationResult.Rejected("插件签名密钥已被吊销。");
        }

        var deniedPermission = manifest.Permissions.FirstOrDefault(
            permission => !_options.AllowedPermissions.Contains(permission));
        if (deniedPermission is not null) {
            return PluginPackageVerificationResult.Rejected($"宿主未授予插件权限：{deniedPermission}。");
        }

        var file = new FileInfo(assemblyPath);
        if (!file.Exists) {
            return PluginPackageVerificationResult.Rejected("插件程序集不存在。");
        }

        if (file.Length is <= 0 || file.Length > _options.MaximumAssemblyBytes) {
            return PluginPackageVerificationResult.Rejected("插件程序集长度无效。");
        }

        await using var stream = new FileStream(
            assemblyPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        if (!TryDecodeHex(manifest.AssemblySha256, out var declaredHash) ||
            !CryptographicOperations.FixedTimeEquals(hash, declaredHash)) {
            return PluginPackageVerificationResult.Rejected("插件程序集摘要不匹配，文件可能已被篡改。");
        }

        var publicKeyPath = Path.Combine(
            Path.GetFullPath(_options.TrustDirectory),
            manifest.SigningKeyId + ".pem");
        if (!File.Exists(publicKeyPath)) {
            return PluginPackageVerificationResult.Rejected("插件签名密钥不在宿主信任库中。");
        }

        try {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(await File.ReadAllTextAsync(publicKeyPath, cancellationToken)
                .ConfigureAwait(false));
            var payload = Encoding.UTF8.GetBytes(BuildSignaturePayload(manifest));
            var signature = Convert.FromBase64String(manifest.Signature);
            return rsa.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss)
                ? PluginPackageVerificationResult.Trusted()
                : PluginPackageVerificationResult.Rejected("插件清单签名无效。");
        }
        catch (Exception exception) when (
            exception is CryptographicException or FormatException or IOException) {
            return PluginPackageVerificationResult.Rejected($"插件签名无法验证：{exception.Message}");
        }
    }

    /// <summary>构造签发工具必须采用的规范签名载荷。</summary>
    public static string BuildSignaturePayload(PluginManifest manifest) {
        return JsonSerializer.Serialize(new {
            manifest.PluginKey,
            manifest.Name,
            manifest.Version,
            manifest.MinimumHostVersion,
            manifest.ContractMajorVersion,
            manifest.EntryPoint,
            Capabilities = manifest.Capabilities.OrderBy(
                static capability => capability,
                StringComparer.Ordinal).ToArray(),
            Permissions = manifest.Permissions.OrderBy(
                static permission => permission,
                StringComparer.Ordinal).ToArray(),
            manifest.SigningKeyId,
            AssemblySha256 = manifest.AssemblySha256.ToUpperInvariant()
        });
    }

    private static bool TryDecodeHex(string value, out byte[] bytes) {
        bytes = [];
        if (value.Length != 64) {
            return false;
        }

        try {
            bytes = Convert.FromHexString(value);
            return true;
        }
        catch (FormatException) {
            return false;
        }
    }

    private static bool IsValidKeyId(string keyId) {
        return !string.IsNullOrWhiteSpace(keyId) &&
               keyId.Length <= 128 &&
               keyId.All(static character =>
                   char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
    }
}

/// <summary>仅供测试使用的显式不签名验证器。</summary>
public sealed class DevelopmentPluginPackageVerifier : IPluginPackageVerifier {
    /// <inheritdoc />
    public ValueTask<PluginPackageVerificationResult> VerifyAsync(
        PluginManifest manifest,
        string assemblyPath,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(PluginPackageVerificationResult.Trusted());
    }
}
