using System.Diagnostics;
using System.Text.Json;
using JayTom.Dws.Application.Packages;
using JayTom.Dws.Models.Package;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Legacy.Contracts.Packages;

namespace JayTom.Dws.Tests.Performance;

/// <summary>对核心索引和条码匹配热路径建立宽松、可重复的性能回归门禁。</summary>
public sealed class PerformanceBaselineTests
{
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>一万次会话添加、索引查询和移除不得退化为全表扫描。</summary>
    [Fact]
    public void Package_session_indexed_operations_stay_within_budget()
    {
        var budget = ReadBudget("packageSessionIndexedOperations");
        var store = new PackageSessionStore();
        var origin = new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Local);
        var elapsed = Stopwatch.StartNew();

        for (var index = 0; index < budget.OperationCount; index++)
        {
            Assert.True(store.TryAddPackage(new PackageInfo
            {
                Id = index + 1,
                CreateTime = origin.AddTicks(index)
            }, []));
        }
        for (var index = 0; index < budget.OperationCount; index++)
        {
            Assert.NotNull(store.GetPackageById(index + 1));
        }
        for (var index = 0; index < budget.OperationCount; index++)
        {
            Assert.True(store.RemovePackage(origin.AddTicks(index), "performance-baseline"));
        }

        elapsed.Stop();
        Assert.True(
            elapsed.ElapsedMilliseconds <= budget.MaximumElapsedMilliseconds,
            $"Indexed session operations took {elapsed.ElapsedMilliseconds} ms; budget is {budget.MaximumElapsedMilliseconds} ms.");
    }

    /// <summary>连续条码匹配在稳定有序索引上保持线性总吞吐，不随历史会话二次排序。</summary>
    [Fact]
    public void Barcode_pipeline_stays_within_budget()
    {
        var budget = ReadBudget("packageBarcodePipeline");
        var store = new PackageSessionStore();
        var origin = new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Local);
        for (var index = 0; index < budget.OperationCount; index++)
        {
            store.AddPackage(new PackageInfo
            {
                Id = index + 1,
                CreateTime = origin.AddMilliseconds(index)
            }, []);
        }

        var elapsed = Stopwatch.StartNew();
        for (var index = 0; index < budget.OperationCount; index++)
        {
            DateTime observedAt = origin.AddMilliseconds(index + 200);
            PackageInfo? bound = store.TryBindBarcode(
                observedAt,
                BarcodeQueueOrderEnum.TimeAscending,
                true,
                0,
                5000,
                null,
                observedAt,
                package => package.BarCodeInfo = new BarCodeInfoModel
                {
                    Barcode = $"JT-PERF-{index}",
                    ScanTime = observedAt,
                    BindTime = observedAt
                });
            Assert.NotNull(bound);
        }

        elapsed.Stop();
        Assert.True(
            elapsed.ElapsedMilliseconds <= budget.MaximumElapsedMilliseconds,
            $"Barcode pipeline took {elapsed.ElapsedMilliseconds} ms; budget is {budget.MaximumElapsedMilliseconds} ms.");
        store.ClearAllPackages();
    }

    /// <summary>读取一个命名性能预算。</summary>
    private static (int OperationCount, long MaximumElapsedMilliseconds) ReadBudget(string sectionName)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "eng",
            "PerformanceBudget.json")));
        JsonElement section = document.RootElement.GetProperty(sectionName);
        return (
            section.GetProperty("operationCount").GetInt32(),
            section.GetProperty("maximumElapsedMilliseconds").GetInt64());
    }

    /// <summary>从测试输出目录向上定位仓库根目录。</summary>
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "JayTom.Dws.sln")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

}
