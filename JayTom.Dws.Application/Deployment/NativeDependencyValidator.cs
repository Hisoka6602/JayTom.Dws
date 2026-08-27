using System.Security.Cryptography;
using System.Text.Json;
using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Application.Deployment;

/// <summary>在启动前验证原生依赖的路径、大小与 SHA-256 完整性。</summary>
public sealed class NativeDependencyValidator
{
    /// <summary>允许的最大清单字节数。</summary>
    private const int MaximumManifestBytes = 1024 * 1024;
    /// <summary>原生清单使用 camelCase，同时保持严格强类型反序列化。</summary>
    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>验证指定发布根目录中的原生依赖。</summary>
    public async Task<OperationResult<int>> ValidateAsync(
        string deploymentRoot,
        string manifestPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deploymentRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);
        string root = Path.GetFullPath(deploymentRoot);
        string manifestFullPath = Path.GetFullPath(manifestPath);
        var manifestInfo = new FileInfo(manifestFullPath);
        if (!manifestInfo.Exists || manifestInfo.Length is <= 0 or > MaximumManifestBytes)
        {
            return OperationResult<int>.Failure(
                new Error("native.manifest.invalid", "原生依赖清单不存在、为空或超过大小限制。"));
        }

        await using FileStream manifestStream = new(
            manifestFullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        NativeDependencyManifest? manifest = await JsonSerializer
            .DeserializeAsync<NativeDependencyManifest>(
                manifestStream,
                ManifestJsonOptions,
                cancellationToken)
            .ConfigureAwait(false);
        if (manifest is null || !manifest.Rid.Equals("win-x64", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<int>.Failure(
                new Error("native.rid.unsupported", "原生依赖清单不是受支持的 win-x64 清单。"));
        }

        foreach (NativeDependencyAsset asset in manifest.Assets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string fullPath = Path.GetFullPath(Path.Combine(root, asset.RelativePath));
            if (!IsWithinRoot(root, fullPath))
            {
                return OperationResult<int>.Failure(
                    new Error("native.path.invalid", $"原生依赖路径越界：{asset.Name}"));
            }

            var fileInfo = new FileInfo(fullPath);
            if (!fileInfo.Exists || fileInfo.Length != asset.Length)
            {
                return OperationResult<int>.Failure(
                    new Error("native.file.missing", $"原生依赖缺失或长度不匹配：{asset.Name}"));
            }

            await using FileStream assetStream = new(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            byte[] hash = await SHA256.HashDataAsync(assetStream, cancellationToken).ConfigureAwait(false);
            // DWS-HEX-COMPACT: SHA-256 清单按协议约定使用无分隔小写十六进制。
            if (!Convert.ToHexStringLower(hash).Equals(asset.Sha256, StringComparison.Ordinal))
            {
                return OperationResult<int>.Failure(
                    new Error("native.hash.mismatch", $"原生依赖校验和不匹配：{asset.Name}"));
            }
        }

        return OperationResult<int>.Success(manifest.Assets.Count);
    }

    /// <summary>判断规范化路径是否位于指定发布根目录中。</summary>
    private static bool IsWithinRoot(string root, string candidate)
    {
        string rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
    }
}
