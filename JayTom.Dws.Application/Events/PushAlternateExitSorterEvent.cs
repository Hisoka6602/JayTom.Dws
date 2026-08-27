using JayTom.Dws.Legacy.Contracts.Packages;

namespace JayTom.Dws.Application.Events;

/// <summary>表示包裹因锁格需要改投备用格口。</summary>
public sealed class PushAlternateExitSorterEvent
{
    /// <summary>获取包裹信息。</summary>
    public PackageInfo PackageInfo { get; init; } = new();
    /// <summary>获取原格口标识。</summary>
    public long OriginalExitId { get; init; }
    /// <summary>获取原格口名称。</summary>
    public string OriginalExitName { get; init; } = string.Empty;
    /// <summary>获取锁格时间。</summary>
    public DateTime LockTime { get; init; }
}
