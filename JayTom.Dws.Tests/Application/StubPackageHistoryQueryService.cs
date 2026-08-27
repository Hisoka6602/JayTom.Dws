using JayTom.Dws.Application.PackageHistory;

namespace JayTom.Dws.Tests.Application;

/// <summary>记录历史查询调用参数的测试读取边界。</summary>
internal sealed class StubPackageHistoryQueryService : IPackageHistoryQueryService
{
    /// <summary>固定查询结果。</summary>
    internal PackageHistoryPage Result { get; } = new(0, []);

    /// <summary>最近一次筛选条件。</summary>
    internal PackageHistoryQuery? LastFilter { get; private set; }

    /// <summary>最近一次页码。</summary>
    internal int LastPageIndex { get; private set; }

    /// <summary>最近一次页大小。</summary>
    internal int LastPageSize { get; private set; }

    /// <summary>记录查询参数并返回固定结果。</summary>
    public Task<PackageHistoryPage> SearchAsync(
        PackageHistoryQuery query,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        LastFilter = query;
        LastPageIndex = pageIndex;
        LastPageSize = pageSize;
        return Task.FromResult(Result);
    }

    /// <summary>本测试替身不返回明细。</summary>
    public Task<PackageHistoryItem?> FindByTimestampAsync(
        long timestamp,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<PackageHistoryItem?>(null);
}
