using System.ComponentModel;

namespace JayTom.Dws.Application.Events;

/// <summary>定义包裹无法正常分拣的稳定业务原因。</summary>
public enum PackageAbnormalSortingType
{
    /// <summary>正常分拣。</summary>
    [Description("正常分拣")] None,
    /// <summary>网络超时。</summary>
    [Description("网络超时")] NetworkTimeout,
    /// <summary>接口访问异常。</summary>
    [Description("Api异常访问")] ApiAccessError,
    /// <summary>未识别条码。</summary>
    [Description("无条码")] NoRead,
    /// <summary>识别到多个条码。</summary>
    [Description("多条码识别")] MultipleBarCode,
    /// <summary>没有分拣指令。</summary>
    [Description("无分拣指令")] NoSortingInstruction,
    /// <summary>没有物理格口。</summary>
    [Description("无物理格口")] NoPhysicalMailbox,
    /// <summary>格口已锁定。</summary>
    [Description("锁格")] LockExit,
    /// <summary>检测到叠包。</summary>
    [Description("叠包")] StackedPackage,
    /// <summary>非本机构条码。</summary>
    [Description("非本机构条码")] PostNonLocalBarcode,
    /// <summary>找不到段道。</summary>
    [Description("查不到段道")] PostSegmentNotFound,
    /// <summary>未命中规则。</summary>
    [Description("未命中规则")] UnmatchedRule,
    /// <summary>包裹距离过近。</summary>
    [Description("距离过近")] DistanceTooClose,
    /// <summary>车号不匹配。</summary>
    [Description("车号不匹配")] VehicleNumberMismatch,
    /// <summary>线速度未稳定。</summary>
    [Description("线速度未稳定放包")] UnstableLineSpeed
}
