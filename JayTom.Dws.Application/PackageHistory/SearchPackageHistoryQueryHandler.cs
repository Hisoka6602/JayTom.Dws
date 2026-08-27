using JayTom.Dws.Application.UseCases;
using JayTom.Dws.Abstractions.Observability;
using System.Diagnostics;

namespace JayTom.Dws.Application.PackageHistory;

/// <summary>处理历史包裹应用查询。</summary>
public sealed class SearchPackageHistoryQueryHandler :
    IApplicationQueryHandler<SearchPackageHistoryQuery, PackageHistoryPage>
{
    /// <summary>历史包裹读取服务。</summary>
    private readonly IPackageHistoryQueryService _service;
    /// <summary>历史包裹查询输入校验器。</summary>
    private readonly IApplicationRequestValidator<SearchPackageHistoryQuery> _validator;

    /// <summary>创建查询处理器。</summary>
    public SearchPackageHistoryQueryHandler(
        IPackageHistoryQueryService service,
        IApplicationRequestValidator<SearchPackageHistoryQuery> validator)
    {
        _service = service;
        _validator = validator;
    }

    /// <summary>校验分页参数并执行历史包裹查询。</summary>
    public async Task<PackageHistoryPage> HandleAsync(
        SearchPackageHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.Filter);
        using CorrelationScope correlation = CorrelationContext.Begin(CorrelationContext.CurrentValueText);
        using Activity? activity = DwsDiagnostics.StartActivity("package-history.search");
        long started = Stopwatch.GetTimestamp();
        try
        {
            var errors = _validator.Validate(query);
            if (errors.Count > 0)
            {
                throw new ArgumentException(errors[0].Message, nameof(query));
            }
            PackageHistoryPage result = await _service.SearchAsync(
                query.Filter,
                query.PageIndex,
                query.PageSize,
                cancellationToken).ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
            DwsDiagnostics.RecordOperation(
                "package-history.search",
                true,
                Stopwatch.GetElapsedTime(started));
            return result;
        }
        catch
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            DwsDiagnostics.RecordOperation(
                "package-history.search",
                false,
                Stopwatch.GetElapsedTime(started));
            throw;
        }
    }
}
