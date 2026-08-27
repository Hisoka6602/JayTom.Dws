using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Legacy.Contracts.Packages;

namespace JayTom.Dws.Application.Events;

/// <summary>表示包裹处理流程到达一个业务触发位置。</summary>
public sealed class TriggerPositionEvent
{
    /// <summary>获取触发位置。</summary>
    public TriggerPositionEnum TriggerPosition { get; init; }

    /// <summary>获取前置处理是否成功。</summary>
    public bool IsSuccess { get; init; }

    /// <summary>获取关联包裹。</summary>
    public PackageInfo? PackageInfo { get; init; }

    /// <summary>获取触发说明。</summary>
    public string Description { get; init; } = string.Empty;
}
