using System.Linq.Expressions;
using JayTom.Dws.Abstractions.Threading;
using JayTom.Dws.Models.Package;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalData;

namespace JayTom.Dws.Application.PackageHistory;

/// <summary>
/// 使用包裹仓储实现历史记录查询用例。
/// </summary>
public sealed class PackageHistoryQueryService : IPackageHistoryQueryService {
    /// <summary>持有包裹数据的持久化边界。</summary>
    private readonly IPackageRepository _repository;

    /// <summary>创建历史包裹查询服务。</summary>
    public PackageHistoryQueryService(IPackageRepository repository) {
        _repository = repository;
    }

    /// <summary>按筛选条件读取一页历史包裹。</summary>
    public Task<PackageHistoryPage> SearchAsync(
        PackageHistoryQuery query,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        return LowPriorityWorkScheduler.Shared.RunAsync(
            () => SearchCoreAsync(query, pageIndex, pageSize, cancellationToken), cancellationToken);
    }

    /// <summary>在低优先级工作线程上查询和映射，不把同步 SQLite 读取带回 UI 线程。</summary>
    private async Task<PackageHistoryPage> SearchCoreAsync(
        PackageHistoryQuery query, int pageIndex, int pageSize, CancellationToken cancellationToken) {
        if (query.PackageTimestamp is { } timestamp) {
            var detail = await FindCoreAsync(timestamp, cancellationToken);
            return new PackageHistoryPage(detail is null ? 0 : 1, detail is null ? [] : [detail]);
        }
        var predicate = CreatePredicate(query);
        var total = query.IncludeTotal
            ? await _repository.Total(predicate, cancellationToken)
            : -1;
        cancellationToken.ThrowIfCancellationRequested();
        if (total == 0) {
            return new PackageHistoryPage(0, Array.Empty<PackageHistoryItem>());
        }

        var result = await _repository.SelectPackageOrderByDescending(
                predicate,
                item => item.PackageCreateTime,
                pageIndex,
                pageSize,
                cancellationToken, includeUploadDetails: false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!result.Key) throw new InvalidOperationException("历史包裹读取失败，请稍后重试。");
        return new PackageHistoryPage(
            total,
            result.Key
                ? Array.AsReadOnly(result.Value.Select(PackageHistoryMapper.Map).ToArray())
                : Array.Empty<PackageHistoryItem>());
    }

    /// <summary>按包裹时间戳读取完整包裹信息。</summary>
    public Task<PackageHistoryItem?> FindByTimestampAsync(
        long timestamp,
        CancellationToken cancellationToken = default) =>
        LowPriorityWorkScheduler.Shared.RunAsync(() => FindCoreAsync(timestamp, cancellationToken), cancellationToken);

    /// <summary>在历史读取线程上查询单个包裹详情。</summary>
    private async Task<PackageHistoryItem?> FindCoreAsync(long timestamp, CancellationToken cancellationToken) {
        var result = await _repository.FirstOrDefaultInfo(
                item => item.PackageTimestamped == timestamp,
                cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (!result.Key) throw new InvalidOperationException("包裹详情读取失败，请稍后重试。");
        return result.Key && result.Value is not null
            ? PackageHistoryMapper.Map(result.Value)
            : null;
    }

    /// <summary>将稳定查询对象转换为持久化适配器能够翻译的表达式。</summary>
    private static Expression<Func<PackageInfoModel, bool>> CreatePredicate(PackageHistoryQuery query) =>
        item => item.BarCodeInfo != null &&
                (query.StartTime == null || item.BarCodeInfo.ScanTime >= query.StartTime) &&
                (query.EndTime == null || item.BarCodeInfo.ScanTime <= query.EndTime) &&
                (string.IsNullOrWhiteSpace(query.Barcode) || item.BarCodeInfo.Barcode.Contains(query.Barcode)) &&
                (string.IsNullOrWhiteSpace(query.PhysicalExit) ||
                 item.ExitInfo != null && item.ExitInfo.PhysicalExit == query.PhysicalExit) &&
                (query.MinWeight <= 0 ||
                 item.WeightInfo != null && item.WeightInfo.FormattedWeight >= query.MinWeight) &&
                (query.MaxWeight <= 0 ||
                 item.WeightInfo != null && item.WeightInfo.FormattedWeight <= query.MaxWeight) &&
                (query.UploadStatus == null ||
                 item.UploadInfo != null && item.UploadInfo.RequestStatus == query.UploadStatus);
}
