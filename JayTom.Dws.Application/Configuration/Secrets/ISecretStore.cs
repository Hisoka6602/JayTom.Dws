// DWS-COHESIVE-CONTRACTS: 秘密引用与存储端口共同构成一个原子公共契约。
using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Application.Configuration.Secrets;

/// <summary>表示设置中可安全保存的秘密引用。</summary>
public readonly record struct SecretReference(string Value);

/// <summary>将敏感值与普通设置分离保存。</summary>
public interface ISecretStore {
    /// <summary>保存秘密并返回不含明文的引用。</summary>
    Task<OperationResult<SecretReference>> SetAsync(
        string purpose,
        ReadOnlyMemory<char> secret,
        CancellationToken cancellationToken);

    /// <summary>按引用读取秘密。</summary>
    Task<OperationResult<string>> GetAsync(
        SecretReference reference,
        CancellationToken cancellationToken);

    /// <summary>删除秘密。</summary>
    Task<OperationResult<bool>> DeleteAsync(
        SecretReference reference,
        CancellationToken cancellationToken);
}
