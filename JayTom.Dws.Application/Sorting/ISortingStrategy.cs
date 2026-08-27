using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Application.Sorting;

/// <summary>定义单一分拣策略的应用层端口。</summary>
public interface ISortingStrategy
{
    /// <summary>获取该实现负责的策略类型。</summary>
    SortingStrategyKind Kind { get; }

    /// <summary>异步计算分拣决策，并遵守调用方的取消契约。</summary>
    Task<OperationResult<SortingDecision>> EvaluateAsync(
        SortingRequest request,
        CancellationToken cancellationToken);
}
