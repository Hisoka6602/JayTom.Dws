namespace JayTom.Dws.Legacy.Contracts.Packages;

/// <summary>在绑定决策时保存候选包裹与时间，避免事后读取变化的会话误判原因。</summary>
public sealed record BarcodeAssignmentRejection(
    BarcodeAssignmentRejectionReason Reason,
    DateTime ObservedAt,
    DateTime ProcessingAt,
    DateTime? PackageCreatedAt = null,
    long? PackageId = null)
{
    /// <summary>决策当时原包裹的唯一标识；小车编号循环复用时仍可关联本次创建。</summary>
    public long? PackageTimestamp { get; init; }
    /// <summary>扫码观测时间减去候选包裹创建时间；赋值窗口使用此间隔。</summary>
    public decimal? ObservedAgeMilliseconds => PackageCreatedAt is { } createdAt
        ? (ObservedAt - createdAt).Ticks / (decimal)TimeSpan.TicksPerMillisecond : null;

    /// <summary>实际处理时包裹的年龄，用于区分排队期间的过期。</summary>
    public decimal? ProcessingAgeMilliseconds => PackageCreatedAt is { } createdAt
        ? (ProcessingAt - createdAt).Ticks / (decimal)TimeSpan.TicksPerMillisecond : null;

    /// <summary>从设备观测到程序开始绑定的延迟。</summary>
    public decimal ProcessingDelayMilliseconds =>
        (ProcessingAt - ObservedAt).Ticks / (decimal)TimeSpan.TicksPerMillisecond;
}
