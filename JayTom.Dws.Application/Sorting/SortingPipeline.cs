using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Application.Sorting;

/// <summary>编排策略决策和协议提交，并统一失败、取消与超时语义。</summary>
public sealed class SortingPipeline
{
    /// <summary>策略注册表。</summary>
    private readonly SortingStrategyRegistry _strategyRegistry;

    /// <summary>厂商协议适配端口。</summary>
    private readonly ISortingProtocolAdapter _protocolAdapter;

    /// <summary>创建分拣应用层管道。</summary>
    public SortingPipeline(
        SortingStrategyRegistry strategyRegistry,
        ISortingProtocolAdapter protocolAdapter)
    {
        _strategyRegistry = strategyRegistry ?? throw new ArgumentNullException(nameof(strategyRegistry));
        _protocolAdapter = protocolAdapter ?? throw new ArgumentNullException(nameof(protocolAdapter));
    }

    /// <summary>在统一超时范围内完成策略决策和协议提交。</summary>
    public async Task<OperationResult<SortingDispatchReceipt>> ExecuteAsync(
        SortingRequest request,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "分拣超时必须大于零。");
        }
        if (!_strategyRegistry.TryResolve(request.Strategy, out ISortingStrategy? strategy) ||
            strategy is null)
        {
            return OperationResult<SortingDispatchReceipt>.Failure(
                "sorting.strategy_not_registered",
                $"未注册分拣策略：{request.Strategy}。");
        }

        using CancellationTokenSource timeoutSource = new(timeout);
        using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);
        try
        {
            OperationResult<SortingDecision> decisionResult =
                await strategy.EvaluateAsync(request, linkedSource.Token).ConfigureAwait(false);
            if (!decisionResult.IsSuccess || decisionResult.Value is null)
            {
                return OperationResult<SortingDispatchReceipt>.Failure(
                    decisionResult.ErrorCode,
                    decisionResult.ErrorMessage);
            }

            SortingDecision decision = decisionResult.Value;
            var command = new SortingProtocolCommand(
                request.PackageId,
                request.Barcode,
                decision.ExitId,
                decision.Instructions,
                TimeSpan.Zero);
            return await _protocolAdapter.SendAsync(command, linkedSource.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return OperationResult<SortingDispatchReceipt>.Failure(
                "sorting.cancelled",
                "分拣操作已取消。");
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            return OperationResult<SortingDispatchReceipt>.Failure(
                "sorting.timeout",
                "分拣操作已超时。");
        }
        catch (Exception exception)
        {
            return OperationResult<SortingDispatchReceipt>.Failure(
                "sorting.unexpected_failure",
                exception.Message);
        }
    }
}
