using JayTom.Dws.Abstractions.Results;
using JayTom.Dws.Application.Sorting;
using JayTom.Dws.Tests.TestDoubles;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证分拣应用层管道的端到端契约。</summary>
public sealed class SortingPipelineTests
{
    /// <summary>已注册策略的决策会转换为稳定协议命令并返回回执。</summary>
    [Fact]
    public async Task ExecuteAsync_routes_decision_to_protocol_adapter()
    {
        var strategy = new StubSortingStrategy(
            SortingStrategyKind.Barcode,
            (_, _) => Task.FromResult(OperationResult<SortingDecision>.Success(
                new SortingDecision(8, ["OPEN:8"]))));
        var adapter = new RecordingSortingProtocolAdapter();
        var pipeline = new SortingPipeline(new SortingStrategyRegistry([strategy]), adapter);

        OperationResult<SortingDispatchReceipt> result = await pipeline.ExecuteAsync(
            CreateRequest(SortingStrategyKind.Barcode),
            TimeSpan.FromSeconds(1));

        Assert.True(result.IsSuccess);
        Assert.NotNull(adapter.Command);
        Assert.Equal(42, adapter.Command.PackageId);
        Assert.Equal(8, adapter.Command.ExitId);
        Assert.Equal("OPEN:8", adapter.Command.Instructions[0]);
    }

    /// <summary>未注册策略返回稳定错误且不会触发协议适配器。</summary>
    [Fact]
    public async Task ExecuteAsync_returns_stable_error_for_missing_strategy()
    {
        var adapter = new RecordingSortingProtocolAdapter();
        var pipeline = new SortingPipeline(new SortingStrategyRegistry([]), adapter);

        OperationResult<SortingDispatchReceipt> result = await pipeline.ExecuteAsync(
            CreateRequest(SortingStrategyKind.Api),
            TimeSpan.FromSeconds(1));

        Assert.False(result.IsSuccess);
        Assert.Equal("sorting.strategy_not_registered", result.ErrorCode);
        Assert.Null(adapter.Command);
    }

    /// <summary>策略的预期失败通过统一结果直接传递且不会发送指令。</summary>
    [Fact]
    public async Task ExecuteAsync_propagates_strategy_failure()
    {
        var strategy = new StubSortingStrategy(
            SortingStrategyKind.Weight,
            (_, _) => Task.FromResult(OperationResult<SortingDecision>.Failure(
                "sorting.no_weight_rule",
                "没有匹配的重量规则。")));
        var adapter = new RecordingSortingProtocolAdapter();
        var pipeline = new SortingPipeline(new SortingStrategyRegistry([strategy]), adapter);

        OperationResult<SortingDispatchReceipt> result = await pipeline.ExecuteAsync(
            CreateRequest(SortingStrategyKind.Weight),
            TimeSpan.FromSeconds(1));

        Assert.False(result.IsSuccess);
        Assert.Equal("sorting.no_weight_rule", result.ErrorCode);
        Assert.Null(adapter.Command);
    }

    /// <summary>调用方取消会传播到策略并转换为统一取消结果。</summary>
    [Fact]
    public async Task ExecuteAsync_honors_caller_cancellation()
    {
        var strategy = new StubSortingStrategy(
            SortingStrategyKind.Ocr,
            async (_, token) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return OperationResult<SortingDecision>.Success(new SortingDecision(1, []));
            });
        var pipeline = new SortingPipeline(
            new SortingStrategyRegistry([strategy]),
            new RecordingSortingProtocolAdapter());
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        OperationResult<SortingDispatchReceipt> result = await pipeline.ExecuteAsync(
            CreateRequest(SortingStrategyKind.Ocr),
            TimeSpan.FromSeconds(1),
            cancellation.Token);

        Assert.False(result.IsSuccess);
        Assert.Equal("sorting.cancelled", result.ErrorCode);
    }

    /// <summary>总处理时限会取消策略并转换为统一超时结果。</summary>
    [Fact]
    public async Task ExecuteAsync_enforces_total_timeout()
    {
        var strategy = new StubSortingStrategy(
            SortingStrategyKind.Logistics,
            async (_, token) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return OperationResult<SortingDecision>.Success(new SortingDecision(1, []));
            });
        var pipeline = new SortingPipeline(
            new SortingStrategyRegistry([strategy]),
            new RecordingSortingProtocolAdapter());

        OperationResult<SortingDispatchReceipt> result = await pipeline.ExecuteAsync(
            CreateRequest(SortingStrategyKind.Logistics),
            TimeSpan.FromMilliseconds(20));

        Assert.False(result.IsSuccess);
        Assert.Equal("sorting.timeout", result.ErrorCode);
    }

    /// <summary>协议层失败通过同一种结果类型返回给调用方。</summary>
    [Fact]
    public async Task ExecuteAsync_propagates_protocol_failure()
    {
        var strategy = new StubSortingStrategy(
            SortingStrategyKind.Volume,
            (_, _) => Task.FromResult(OperationResult<SortingDecision>.Success(
                new SortingDecision(3, ["OPEN:3"]))));
        var adapter = new RecordingSortingProtocolAdapter { FailureCode = "sorting.protocol_rejected" };
        var pipeline = new SortingPipeline(new SortingStrategyRegistry([strategy]), adapter);

        OperationResult<SortingDispatchReceipt> result = await pipeline.ExecuteAsync(
            CreateRequest(SortingStrategyKind.Volume),
            TimeSpan.FromSeconds(1));

        Assert.False(result.IsSuccess);
        Assert.Equal("sorting.protocol_rejected", result.ErrorCode);
    }

    /// <summary>创建稳定的测试输入。</summary>
    private static SortingRequest CreateRequest(SortingStrategyKind kind) =>
        new(42, "JT00042", kind, DateTimeOffset.UnixEpoch,
            new Dictionary<string, string>());
}
