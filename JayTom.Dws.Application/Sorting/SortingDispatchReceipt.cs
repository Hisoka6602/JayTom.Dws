namespace JayTom.Dws.Application.Sorting;

/// <summary>表示协议适配器已经接收分拣命令的回执。</summary>
/// <param name="PackageId">包裹的稳定标识。</param>
/// <param name="ExitId">目标格口标识。</param>
/// <param name="AcceptedAt">适配器接收命令的时间。</param>
public sealed record SortingDispatchReceipt(
    long PackageId,
    long ExitId,
    DateTimeOffset AcceptedAt);
