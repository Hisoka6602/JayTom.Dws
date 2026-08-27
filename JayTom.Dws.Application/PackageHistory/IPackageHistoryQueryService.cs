namespace JayTom.Dws.Application.PackageHistory;

/// <summary>
/// 提供历史包裹读取用例，并隐藏关系查询表达式。
/// </summary>
public interface IPackageHistoryQueryService {
    /// <summary>按筛选条件读取一页历史包裹。</summary>
    Task<PackageHistoryPage> SearchAsync(
        PackageHistoryQuery query,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>按包裹时间戳读取完整包裹信息。</summary>
    Task<PackageHistoryItem?> FindByTimestampAsync(
        long timestamp,
        CancellationToken cancellationToken = default);
}
