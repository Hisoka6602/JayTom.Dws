namespace JayTom.Dws.Application.Events;

/// <summary>定义分拣流程中的格口更新阶段。</summary>
public enum SortingExitType
{
    /// <summary>物理格口。</summary>
    PhysicalExit,
    /// <summary>理论格口。</summary>
    TheoreticalExit
}
