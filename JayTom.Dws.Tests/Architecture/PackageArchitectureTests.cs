namespace JayTom.Dws.Tests.Architecture;

/// <summary>锁定包裹聚合、会话索引和性能门禁的物理架构。</summary>
public sealed class PackageArchitectureTests
{
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>注册表和聚合实体必须由不同源文件拥有。</summary>
    [Fact]
    public void Package_registry_and_aggregate_are_physically_separated()
    {
        string registry = Read("JayTom.Dws.Legacy.Contracts", "Manager", "PackageSessionRegistry.cs");
        string aggregate = Read("JayTom.Dws.Legacy.Contracts", "Packages", "PackageInfo.cs");

        Assert.Contains("sealed class PackageSessionRegistry", registry, StringComparison.Ordinal);
        Assert.DoesNotContain("class PackageInfo {", registry, StringComparison.Ordinal);
        Assert.Contains("class PackageInfo {", aggregate, StringComparison.Ordinal);
        Assert.DoesNotContain("class PackageSessionRegistry {", aggregate, StringComparison.Ordinal);
    }

    /// <summary>热路径使用稳定索引和聚合实例锁，不得恢复每次排序或全局聚合锁。</summary>
    [Fact]
    public void Package_registry_keeps_precomputed_indexes_and_per_aggregate_locks()
    {
        string source = Read("JayTom.Dws.Legacy.Contracts", "Manager", "PackageSessionRegistry.cs");

        Assert.Contains("SortedSet<DateTime> _packageOrder", source, StringComparison.Ordinal);
        Assert.Contains("Dictionary<long, DateTime> _packageIdIndex", source, StringComparison.Ordinal);
        Assert.Contains("lock (package.SyncRoot)", source, StringComparison.Ordinal);
        Assert.Contains("lock (current.SyncRoot)", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".OrderBy(", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".OrderByDescending(", source, StringComparison.Ordinal);
    }

    /// <summary>移除事件必须在图像和计时器释放之前触发，且释放放在 finally 中。</summary>
    [Fact]
    public void Package_removal_notifies_before_releasing_owned_resources()
    {
        string source = Read("JayTom.Dws.Legacy.Contracts", "Manager", "PackageSessionRegistry.cs");
        int invoke = source.IndexOf("PackageRemoved?.Invoke(this, e)", StringComparison.Ordinal);
        int takeImage = source.IndexOf("e.RemovedPackage.TakeImage()?.Dispose()", StringComparison.Ordinal);
        int disposeTimers = source.IndexOf("e.RemovedPackage.DisposeTimers()", StringComparison.Ordinal);

        Assert.True(invoke >= 0 && invoke < takeImage && takeImage < disposeTimers);
        Assert.Contains("finally {", source, StringComparison.Ordinal);
    }

    /// <summary>性能预算和对应自动化门禁必须同时存在。</summary>
    [Fact]
    public void Performance_budget_has_an_executable_regression_gate()
    {
        Assert.True(File.Exists(Path.Combine(RepositoryRoot, "eng", "PerformanceBudget.json")));
        string test = Read("JayTom.Dws.Tests", "Performance", "PerformanceBaselineTests.cs");
        Assert.Contains("Package_session_indexed_operations_stay_within_budget", test, StringComparison.Ordinal);
        Assert.Contains("Barcode_pipeline_stays_within_budget", test, StringComparison.Ordinal);
    }

    /// <summary>后台包裹业务流程必须由 Application 三阶段流水线编排，Client 仅接入运行时事件。</summary>
    [Fact]
    public void Background_flow_uses_application_package_pipeline()
    {
        string pipeline = Read(
            "JayTom.Dws.Application", "PackageProcessing", "PackageProcessingPipeline.cs");
        string backgroundService = Read(
            "JayTom.Dws.Client", "Service", "ProcessingServices", "PackageBackgroundService.cs");

        int acquisition = pipeline.IndexOf("decision = await _acquisition", StringComparison.Ordinal);
        int matching = pipeline.IndexOf("decision = await _matching", StringComparison.Ordinal);
        int completion = pipeline.IndexOf("decision = await _completion", StringComparison.Ordinal);
        Assert.True(acquisition >= 0 && acquisition < matching && matching < completion);
        Assert.Contains("PackageProcessingPipeline", backgroundService, StringComparison.Ordinal);
        Assert.Contains("_packageProcessingPipeline.ExecuteAsync", backgroundService, StringComparison.Ordinal);
    }

    /// <summary>读取仓库内指定源码文件。</summary>
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
