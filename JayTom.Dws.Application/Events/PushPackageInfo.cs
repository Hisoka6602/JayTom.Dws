using JayTom.Dws.Legacy.Contracts.Packages;

namespace JayTom.Dws.Application.Events;

/// <summary>表示可以推送到外围系统的包裹和落格快照。</summary>
public sealed class PushPackageInfo
{
    /// <summary>获取落格信息。</summary>
    public PackageExitUpdateEvent PackageExitUpdateEvent { get; init; } = new();
    /// <summary>获取包裹信息。</summary>
    public PackageInfo PackageInfo { get; init; } = new();
    /// <summary>获取落格信号回调时间。</summary>
    public DateTime? SignalCallbackTime { get; init; }
}
