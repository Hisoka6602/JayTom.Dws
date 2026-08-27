// DWS-COHESIVE-CONTRACTS: 授权标识与授权聚合共同定义领域边界。
namespace JayTom.Dws.Domain.Licensing;

/// <summary>表示稳定授权标识。</summary>
public readonly record struct LicenseId(string Value);

/// <summary>
/// 表示与文件格式和密码算法无关的授权聚合。
/// </summary>
public sealed class LicenseAggregate {
    private readonly HashSet<string> _features;

    /// <summary>创建授权聚合。</summary>
    public LicenseAggregate(
        LicenseId id,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        IEnumerable<string> features) {
        if (expiresAt <= issuedAt) {
            throw new ArgumentOutOfRangeException(nameof(expiresAt));
        }

        Id = id;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        _features = new HashSet<string>(features, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>获取授权标识。</summary>
    public LicenseId Id { get; }

    /// <summary>获取签发时间。</summary>
    public DateTimeOffset IssuedAt { get; }

    /// <summary>获取到期时间。</summary>
    public DateTimeOffset ExpiresAt { get; }

    /// <summary>获取是否已被撤销。</summary>
    public bool IsRevoked { get; private set; }

    /// <summary>判断在指定时间是否有效。</summary>
    public bool IsValidAt(DateTimeOffset timestamp) =>
        !IsRevoked && timestamp >= IssuedAt && timestamp < ExpiresAt;

    /// <summary>判断是否包含授权功能。</summary>
    public bool HasFeature(string feature) => _features.Contains(feature);

    /// <summary>撤销授权。</summary>
    public void Revoke() => IsRevoked = true;
}
