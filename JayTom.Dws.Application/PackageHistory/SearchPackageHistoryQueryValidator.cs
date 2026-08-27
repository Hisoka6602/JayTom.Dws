using JayTom.Dws.Abstractions.Results;
using JayTom.Dws.Application.UseCases;

namespace JayTom.Dws.Application.PackageHistory;

/// <summary>集中校验历史包裹查询分页和筛选范围。</summary>
public sealed class SearchPackageHistoryQueryValidator :
    IApplicationRequestValidator<SearchPackageHistoryQuery>
{
    /// <summary>返回全部稳定的查询输入错误。</summary>
    public IReadOnlyList<Error> Validate(SearchPackageHistoryQuery request)
    {
        var errors = new List<Error>();
        if (request.PageIndex < 0)
        {
            errors.Add(new Error("package_history.invalid_page_index", "页码不能为负数。"));
        }
        if (request.PageSize is < 1 or > 1000)
        {
            errors.Add(new Error("package_history.invalid_page_size", "每页条数必须位于 1 到 1000 之间。"));
        }
        if (request.Filter.StartTime is not null &&
            request.Filter.EndTime is not null &&
            request.Filter.StartTime > request.Filter.EndTime)
        {
            errors.Add(new Error("package_history.invalid_time_range", "开始时间不能晚于结束时间。"));
        }
        if (request.Filter.MinWeight > 0 &&
            request.Filter.MaxWeight > 0 &&
            request.Filter.MinWeight > request.Filter.MaxWeight)
        {
            errors.Add(new Error("package_history.invalid_weight_range", "最小重量不能大于最大重量。"));
        }
        return errors;
    }
}
