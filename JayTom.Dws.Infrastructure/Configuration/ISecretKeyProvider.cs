// DWS-COHESIVE-CONTRACTS: 密钥端口与部署环境实现成对交付。
using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Infrastructure.Configuration;

/// <summary>从操作系统秘密服务或部署环境提供主密钥。</summary>
public interface ISecretKeyProvider {
    /// <summary>获取指定用途的 256 位主密钥。</summary>
    ValueTask<OperationResult<byte[]>> GetKeyAsync(
        string purpose,
        CancellationToken cancellationToken);
}

/// <summary>从部署注入的环境变量读取主密钥，不在代码或配置库中保存密钥。</summary>
internal sealed class EnvironmentSecretKeyProvider : ISecretKeyProvider {
    private readonly string _environmentVariable;

    /// <summary>创建环境主密钥提供器。</summary>
    public EnvironmentSecretKeyProvider(
        string environmentVariable = "DWS_SECRET_MASTER_KEY") {
        _environmentVariable = environmentVariable;
    }

    /// <inheritdoc />
    public ValueTask<OperationResult<byte[]>> GetKeyAsync(
        string purpose,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        var encoded = Environment.GetEnvironmentVariable(_environmentVariable);
        if (string.IsNullOrWhiteSpace(encoded)) {
            return ValueTask.FromResult(OperationResult<byte[]>.Failure(
                "secret.key_missing",
                $"未配置秘密主密钥环境变量 {_environmentVariable}。"));
        }

        try {
            var key = Convert.FromBase64String(encoded);
            return ValueTask.FromResult(key.Length == 32
                ? OperationResult<byte[]>.Success(key)
                : OperationResult<byte[]>.Failure(
                    "secret.key_invalid",
                    "秘密主密钥必须是 32 字节的 Base64 文本。"));
        }
        catch (FormatException) {
            return ValueTask.FromResult(OperationResult<byte[]>.Failure(
                "secret.key_invalid",
                "秘密主密钥不是有效 Base64 文本。"));
        }
    }
}
