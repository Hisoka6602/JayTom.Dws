using JayTom.Dws.Abstractions.Results;
using JayTom.Dws.Application.Sorting;

namespace JayTom.Dws.Tests.TestDoubles;

/// <summary>为应用层分拣管道提供可编程的策略替身。</summary>
internal sealed class StubSortingStrategy : ISortingStrategy
{
    /// <summary>策略回调。</summary>
    private readonly Func<SortingRequest, CancellationToken, Task<OperationResult<SortingDecision>>> _callback;

    /// <summary>创建指定类型的策略替身。</summary>
    public StubSortingStrategy(
        SortingStrategyKind kind,
        Func<SortingRequest, CancellationToken, Task<OperationResult<SortingDecision>>> callback)
    {
        Kind = kind;
        _callback = callback;
    }

    /// <summary>获取策略类型。</summary>
    public SortingStrategyKind Kind { get; }

    /// <summary>执行测试提供的策略回调。</summary>
    public Task<OperationResult<SortingDecision>> EvaluateAsync(
        SortingRequest request,
        CancellationToken cancellationToken) =>
        _callback(request, cancellationToken);
}
