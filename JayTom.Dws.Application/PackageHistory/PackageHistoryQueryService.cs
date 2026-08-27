using System.Linq.Expressions;
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
    public async Task<PackageHistoryPage> SearchAsync(
        PackageHistoryQuery query,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var predicate = CreatePredicate(query);
        var total = await _repository.Total(predicate, cancellationToken).ConfigureAwait(false);
        if (total == 0) {
            return new PackageHistoryPage(0, Array.Empty<PackageHistoryItem>());
        }

        var result = await _repository.SelectPackageOrderByDescending(
                predicate,
                item => item.PackageCreateTime,
                pageIndex,
                pageSize,
                cancellationToken)
            .ConfigureAwait(false);
        return new PackageHistoryPage(
            total,
            result.Key
                ? Array.AsReadOnly(result.Value.Select(PackageHistoryMapper.Map).ToArray())
                : Array.Empty<PackageHistoryItem>());
    }

    /// <summary>按包裹时间戳读取完整包裹信息。</summary>
    public async Task<PackageHistoryItem?> FindByTimestampAsync(
        long timestamp,
        CancellationToken cancellationToken = default) {
        var result = await _repository.FirstOrDefaultInfo(
                item => item.PackageTimestamped == timestamp,
                cancellationToken)
            .ConfigureAwait(false);
        return result.Key && result.Value is not null
            ? PackageHistoryMapper.Map(result.Value)
            : null;
    }

    /// <summary>将稳定查询对象转换为持久化适配器能够翻译的表达式。</summary>
    private static Expression<Func<PackageInfoModel, bool>> CreatePredicate(PackageHistoryQuery query) =>
        item => item.BarCodeInfo != null &&
                item.WeightInfo != null &&
                (query.StartTime == null || item.BarCodeInfo.ScanTime >= query.StartTime) &&
                (query.EndTime == null || item.BarCodeInfo.ScanTime <= query.EndTime) &&
                (string.IsNullOrWhiteSpace(query.Barcode) || item.BarCodeInfo.Barcode.Contains(query.Barcode)) &&
                (string.IsNullOrWhiteSpace(query.PhysicalExit) ||
                 item.ExitInfo != null && item.ExitInfo.PhysicalExit == query.PhysicalExit) &&
                (query.MinWeight <= 0 || item.WeightInfo.FormattedWeight >= query.MinWeight) &&
                (query.MaxWeight <= 0 || item.WeightInfo.FormattedWeight <= query.MaxWeight) &&
                (query.UploadStatus == null ||
                 item.UploadInfo != null && item.UploadInfo.RequestStatus == query.UploadStatus);
}
