using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Application.PackageHistory;

/// <summary>
/// 描述历史包裹页面的查询条件。
/// </summary>
public sealed record PackageHistoryQuery(
    DateTime? StartTime = null,
    DateTime? EndTime = null,
    string? Barcode = null,
    string? PhysicalExit = null,
    decimal MinWeight = 0,
    decimal MaxWeight = 0,
    UploadStatus? UploadStatus = null);

