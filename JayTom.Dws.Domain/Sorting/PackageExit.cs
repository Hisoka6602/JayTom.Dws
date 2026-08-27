// DWS-COHESIVE-CONTRACTS: 格口标识、锁和值聚合共同维护格口不变量。
using JayTom.Dws.Domain.Common;

namespace JayTom.Dws.Domain.Sorting;

/// <summary>表示稳定格口标识。</summary>
public readonly record struct PackageExitId(long Value);

/// <summary>
/// 管理格口启停和锁定不变量的聚合。
/// </summary>
public sealed class PackageExit {
    /// <summary>创建格口聚合。</summary>
    public PackageExit(PackageExitId id, string code) {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Id = id;
        Code = code;
    }

    /// <summary>获取格口标识。</summary>
    public PackageExitId Id { get; }

    /// <summary>获取格口业务代码。</summary>
    public string Code { get; }

    /// <summary>获取格口是否启用。</summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>获取当前锁定信息。</summary>
    public ExitLock? Lock { get; private set; }

    /// <summary>尝试锁定格口。</summary>
    public bool TryLock(string reason, DateTimeOffset lockedAt) {
        if (!IsEnabled || Lock is not null) {
            return false;
        }

        Lock = new ExitLock(reason, lockedAt);
        return true;
    }

    /// <summary>解除格口锁定。</summary>
    public void Unlock() => Lock = null;

    /// <summary>停用格口并清理锁。</summary>
    public void Disable() {
        IsEnabled = false;
        Lock = null;
    }
}

/// <summary>表示格口锁定值对象。</summary>
public sealed record ExitLock(string Reason, DateTimeOffset LockedAt);
