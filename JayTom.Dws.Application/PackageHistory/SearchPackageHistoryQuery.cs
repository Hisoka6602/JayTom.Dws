using JayTom.Dws.Application.UseCases;

namespace JayTom.Dws.Application.PackageHistory;

/// <summary>包含筛选和分页信息的历史包裹应用查询。</summary>
public sealed record SearchPackageHistoryQuery(
    PackageHistoryQuery Filter,
    int PageIndex,
    int PageSize) : IApplicationQuery<PackageHistoryPage>;
