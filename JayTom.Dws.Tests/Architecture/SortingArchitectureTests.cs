namespace JayTom.Dws.Tests.Architecture;

/// <summary>锁定分拣流程的应用边界、策略扩展点和协议适配方向。</summary>
public sealed class SortingArchitectureTests
{
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>旧巨型服务必须把 API 解析与模式分派移交给独立协作者。</summary>
    [Fact]
    public void Default_service_delegates_parsing_and_strategy_dispatch()
    {
        string service = Read("JayTom.Dws.Client", "Service", "Sorting", "DefaultSortingService.cs");

        Assert.True(service.Split('\n').Length < 1900);
        Assert.Contains("ApiSortingRuleEvaluator", service, StringComparison.Ordinal);
        Assert.Contains("LegacySortingStrategyRegistry", service, StringComparison.Ordinal);
        Assert.DoesNotContain("JsonDocument", service, StringComparison.Ordinal);
        Assert.DoesNotContain("switch (CurrentSortingMethod.SortMode)", service, StringComparison.Ordinal);
    }

    /// <summary>策略实现必须通过应用层注册表按类型解析。</summary>
    [Fact]
    public void Strategy_registry_is_an_application_layer_extension_point()
    {
        string registry = Read("JayTom.Dws.Application", "Sorting", "SortingStrategyRegistry.cs");
        string contract = Read("JayTom.Dws.Application", "Sorting", "ISortingStrategy.cs");

        Assert.Contains("IReadOnlyDictionary<SortingStrategyKind, ISortingStrategy>", registry, StringComparison.Ordinal);
        Assert.Contains("TryGetValue", registry, StringComparison.Ordinal);
        Assert.Contains("OperationResult<SortingDecision>", contract, StringComparison.Ordinal);
    }

    /// <summary>格口到连接的解析必须使用启动时构建的字典索引。</summary>
    [Fact]
    public void Connection_resolution_uses_precomputed_snapshot()
    {
        string source = Read(
            "JayTom.Dws.Client",
            "Service",
            "Sorting",
            "DefaultSortingConnectionService.cs");
        int start = source.IndexOf("ResolveConnectionConfig(long exitId)", StringComparison.Ordinal);
        int end = source.IndexOf("ResolveDefaultConnectionName()", start, StringComparison.Ordinal);
        string method = source[start..end];

        Assert.Contains("SortingConnectionLookupSnapshot", source, StringComparison.Ordinal);
        Assert.Contains("ExitConnections", method, StringComparison.Ordinal);
        Assert.Contains("TryGetValue", method, StringComparison.Ordinal);
        Assert.DoesNotContain("FirstOrDefault", method, StringComparison.Ordinal);
    }

    /// <summary>分拣输入、决策和协议命令必须是应用层独立 DTO。</summary>
    [Fact]
    public void Sorting_dtos_are_physically_isolated_in_application_layer()
    {
        Assert.Contains("record SortingRequest", Read("JayTom.Dws.Application", "Sorting", "SortingRequest.cs"), StringComparison.Ordinal);
        Assert.Contains("record SortingDecision", Read("JayTom.Dws.Application", "Sorting", "SortingDecision.cs"), StringComparison.Ordinal);
        Assert.Contains("record SortingProtocolCommand", Read("JayTom.Dws.Application", "Sorting", "SortingProtocolCommand.cs"), StringComparison.Ordinal);
    }

    /// <summary>管道必须用统一结果表达策略失败、取消和总超时。</summary>
    [Fact]
    public void Sorting_pipeline_has_unified_failure_cancellation_and_timeout()
    {
        string pipeline = Read("JayTom.Dws.Application", "Sorting", "SortingPipeline.cs");

        Assert.Contains("OperationResult<SortingDispatchReceipt>", pipeline, StringComparison.Ordinal);
        Assert.Contains("CreateLinkedTokenSource", pipeline, StringComparison.Ordinal);
        Assert.Contains("sorting.cancelled", pipeline, StringComparison.Ordinal);
        Assert.Contains("sorting.timeout", pipeline, StringComparison.Ordinal);
    }

    /// <summary>厂商通讯实现必须位于客户端适配层且具备端到端契约测试。</summary>
    [Fact]
    public void Vendor_protocol_is_implemented_in_adapter_layer_and_contract_tested()
    {
        string adapter = Read(
            "JayTom.Dws.Client",
            "Service",
            "Sorting",
            "SortingConnectionProtocolAdapter.cs");
        string tests = Read("JayTom.Dws.Tests", "Application", "SortingPipelineTests.cs");

        Assert.Contains("ISortingProtocolAdapter", adapter, StringComparison.Ordinal);
        Assert.Contains("ISortingConnectionService", adapter, StringComparison.Ordinal);
        Assert.Contains("ExecuteAsync_routes_decision_to_protocol_adapter", tests, StringComparison.Ordinal);
        Assert.Contains("ExecuteAsync_enforces_total_timeout", tests, StringComparison.Ordinal);
    }

    /// <summary>读取仓库内指定源文件。</summary>
    private static string Read(params string[] segments) =>
        File.ReadAllText(Path.Combine([RepositoryRoot, .. segments]));

    /// <summary>从测试输出目录向上定位仓库根目录。</summary>
    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "JayTom.Dws.sln")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName
               ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
