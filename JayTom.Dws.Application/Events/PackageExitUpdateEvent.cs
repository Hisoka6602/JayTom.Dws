using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Application.Events;

/// <summary>表示包裹的理论或物理格口已经更新。</summary>
public sealed class PackageExitUpdateEvent
{
    /// <summary>获取包裹创建时间。</summary>
    public DateTime CreateTime { get; init; }
    /// <summary>获取包裹关联时间戳。</summary>
    public long Timestamp { get; init; }
    /// <summary>获取格口名称。</summary>
    public string ExitName { get; init; } = string.Empty;
    /// <summary>获取格口标识。</summary>
    public long ExitId { get; init; }
    /// <summary>获取本次更新的格口阶段。</summary>
    public SortingExitType ExitType { get; init; }
    /// <summary>获取包裹异常原因。</summary>
    public PackageAbnormalSortingType PackageAbnormalSortingType { get; init; }
    /// <summary>获取关联指令快照。</summary>
    public IReadOnlyList<InstructionInfoModel>? InstructionInfos { get; init; }
    /// <summary>获取指令类型。</summary>
    public InstructionType InstructionType { get; init; }
    /// <summary>获取格口业务类型。</summary>
    public ExitType Type { get; init; }
}
