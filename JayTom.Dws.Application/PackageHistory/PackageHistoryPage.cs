namespace JayTom.Dws.Application.PackageHistory;

/// <summary>
/// 表示一次历史包裹分页查询结果。
/// </summary>
public sealed record PackageHistoryPage(int Total, IReadOnlyList<PackageHistoryItem> Items);
