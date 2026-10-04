using System.Diagnostics;
using System.Text.Json;
using JayTom.Dws.Application.Packages;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Legacy.Contracts.Packages;
using JayTom.Dws.Models.Package;
using Xunit.Abstractions;

namespace JayTom.Dws.Tests.Performance;

/// <summary>密集小车创建、NoRead 消费及有效条码匹配的吞吐和分配门禁。</summary>
public sealed class LowerMachinePackagePerformanceTests(ITestOutputHelper output)
{
    private static readonly List<PackageTimer> EmptyTimers = [];
    private static readonly DateTime Origin = new(2026, 10, 3, 10, 0, 0, DateTimeKind.Local);

    /// <summary>大量积压下混合消费十万条结果，不因活动包裹增加而退化为扫描。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Dense_lower_machine_mixed_results_stay_within_budget(bool originalCart)
    {
        var (count, maximumMilliseconds) = ReadBudget();
        var store = new PackageSessionStore();
        var total = Stopwatch.StartNew();
        for (var index = 0; index < count; index++)
        {
            Assert.True(store.TryAddPackage(new PackageInfo
            {
                Id = index + 1,
                IsCreatedByLowerMachine = true,
                CreateTime = Origin.AddTicks(index)
            }, EmptyTimers));
        }
        var creationMilliseconds = Milliseconds(total);
        var processing = Stopwatch.StartNew();
        var observedOrigin = Origin.AddSeconds(1);
        for (var index = 0; index < count; index++)
        {
            var observedAt = observedOrigin.AddTicks(index);
            PackageInfo? result;
            if (index % 2 == 0)
            {
                result = originalCart
                    ? store.TryConsumeCameraResultForPackage(Origin.AddTicks(index), observedAt,
                        false, 0, 0, null, observedAt, assignment: null, out _)
                    : store.TryDiscardUnassignedPackage(observedAt,
                        BarcodeQueueOrderEnum.TimeAscending, false, 0, 0, null, observedAt, out _);
            }
            else
            {
                Action<PackageInfo> assignment = package => package.BarCodeInfo = new BarCodeInfoModel
                    {
                        Barcode = "JT-PERF",
                        SerialNumber = "camera-1",
                        ScanTime = observedAt,
                        BindTime = observedAt
                    };
                result = originalCart
                    ? store.TryConsumeCameraResultForPackage(Origin.AddTicks(index), observedAt,
                        false, 0, 0, null, observedAt, assignment, out _)
                    : store.TryBindBarcode(observedAt, BarcodeQueueOrderEnum.TimeAscending,
                        false, 0, 0, null, observedAt, assignment);
                Assert.Same(result, store.GetPackageByBarcodeObservation(observedAt, "camera-1"));
            }
            Assert.NotNull(result);
            Assert.Equal(index + 1L, result.Guid);
        }
        processing.Stop();
        Assert.False(store.HasUnassignedPackage());
        Assert.Equal(count / 2, store.GetPackageCount());
        store.ClearAllPackages();
        total.Stop();

        output.WriteLine($"Original cart correlation: {originalCart}.");
        output.WriteLine($"Created {count:N0} carts in {creationMilliseconds:F2} ms.");
        output.WriteLine($"Consumed {count:N0} mixed results in {Milliseconds(processing):F2} ms " +
                         $"({Milliseconds(processing) * 1000 / count:F2} us/result).");
        output.WriteLine($"Creation, mixed result processing and cleanup: {Milliseconds(total):F2} ms.");
        Assert.Equal(0, store.GetPackageCount());
        Assert.True(total.ElapsedMilliseconds <= maximumMilliseconds,
            $"Dense lower-machine pipeline took {total.ElapsedMilliseconds} ms; budget is {maximumMilliseconds} ms.");
    }

    /// <summary>开启 NoRead 后十万次连续空读都正常完成，活动会话积压不引起全队列扫描。</summary>
    [Fact]
    public void Enabled_NoRead_dense_results_complete_every_original_cart_within_budget()
    {
        var (count, maximumMilliseconds) = ReadBudget();
        var store = new PackageSessionStore();
        var completedCount = 0;
        store.PackageCompleted += (_, _) => completedCount++;
        var total = Stopwatch.StartNew();
        for (var index = 0; index < count; index++)
        {
            Assert.True(store.TryAddPackage(new PackageInfo
            {
                Id = index + 1,
                IsCreatedByLowerMachine = true,
                CreateTime = Origin.AddTicks(index),
                WeightInfo = new WeightInfoModel(),
                VolumeInfo = new VolumeInfoModel()
            }, EmptyTimers));
        }
        var creationMilliseconds = Milliseconds(total);
        var processing = Stopwatch.StartNew();
        Action<PackageInfo> assignment = static package => package.BarCodeInfo = new BarCodeInfoModel
        {
            Barcode = "NoRead",
            SerialNumber = "camera-1",
            Source = SourceType.Camera,
            ScanTime = package.CreateTime.AddSeconds(1),
            BindTime = package.CreateTime.AddSeconds(1)
        };
        for (var index = 0; index < count; index++)
        {
            var createdAt = Origin.AddTicks(index);
            var observedAt = createdAt.AddSeconds(1);
            var result = store.TryConsumeCameraResultForPackage(createdAt, observedAt,
                false, 0, 0, null, observedAt, assignment, out var rejection);
            Assert.NotNull(result);
            Assert.Null(rejection);
            Assert.Equal(index + 1L, result.Guid);
            store.CompletePackage(createdAt);
            Assert.True(result.IsCompleted);
            Assert.Equal("NoRead", result.BarCodeInfo?.Barcode);
            Assert.Same(result, store.GetPackageByBarcodeObservation(observedAt, "camera-1"));
        }
        processing.Stop();
        Assert.Equal(count, completedCount);
        Assert.Equal(count, store.GetPackageCount());
        Assert.False(store.HasUnassignedPackage());
        store.ClearAllPackages();
        total.Stop();

        output.WriteLine($"Created {count:N0} carts for enabled NoRead in {creationMilliseconds:F2} ms.");
        output.WriteLine($"Bound and completed {count:N0} NoRead results in {Milliseconds(processing):F2} ms " +
                         $"({Milliseconds(processing) * 1000 / count:F2} us/result).");
        output.WriteLine($"Enabled NoRead creation, completion and cleanup: {Milliseconds(total):F2} ms.");
        Assert.Equal(0, store.GetPackageCount());
        Assert.True(total.ElapsedMilliseconds <= maximumMilliseconds,
            $"Enabled NoRead pipeline took {total.ElapsedMilliseconds} ms; budget is {maximumMilliseconds} ms.");
    }

    /// <summary>关闭 NoRead 时，消费热路径不分配业务条码或三个空计时器列表。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NoRead_consumption_keeps_managed_allocation_small(bool originalCart)
    {
        const int count = 10000;
        var store = new PackageSessionStore();
        for (var index = 0; index < count; index++)
        {
            store.AddPackage(new PackageInfo
            {
                Id = index + 1,
                CreateTime = Origin.AddTicks(index)
            }, EmptyTimers);
        }
        var observedAt = Origin.AddSeconds(1);
        // 首次调用预热；测量只包含消费动作，不包含创建记录、测试断言和日志输出。
        Assert.NotNull(originalCart
            ? store.TryConsumeCameraResultForPackage(Origin, observedAt, false, 0, 0, null,
                observedAt, assignment: null, out _)
            : store.TryDiscardUnassignedPackage(observedAt,
                BarcodeQueueOrderEnum.TimeAscending, false, 0, 0, null, observedAt, out _));
        var startedBytes = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 1; index < count; index++)
        {
            if (originalCart)
            {
                store.TryConsumeCameraResultForPackage(Origin.AddTicks(index), observedAt,
                    false, 0, 0, null, observedAt, assignment: null, out _);
            }
            else
            {
                store.TryDiscardUnassignedPackage(observedAt,
                    BarcodeQueueOrderEnum.TimeAscending, false, 0, 0, null, observedAt, out _);
            }
        }
        var bytesPerResult = (decimal)(GC.GetAllocatedBytesForCurrentThread() - startedBytes) / (count - 1);

        output.WriteLine($"Original cart correlation: {originalCart}.");
        output.WriteLine($"NoRead consumption allocates {bytesPerResult:F2} managed bytes/result.");
        Assert.Equal(0, store.GetPackageCount());
        Assert.InRange(bytesPerResult, 0, 256);
    }

    /// <summary>以单调时钟刻度计算带单位的耗时，避免精度不明确的浮点时间。</summary>
    private static decimal Milliseconds(Stopwatch stopwatch) =>
        (decimal)stopwatch.ElapsedTicks * 1000 / Stopwatch.Frequency;

    private static (int Count, long MaximumMilliseconds) ReadBudget()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "JayTom.Dws.sln")))
        {
            directory = directory.Parent;
        }
        if (directory is null)
        {
            throw new DirectoryNotFoundException("Could not locate repository root.");
        }
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            directory.FullName, "eng", "PerformanceBudget.json")));
        var section = document.RootElement.GetProperty("packageLowerMachineMixedResults");
        return (section.GetProperty("operationCount").GetInt32(),
            section.GetProperty("maximumElapsedMilliseconds").GetInt64());
    }
}
