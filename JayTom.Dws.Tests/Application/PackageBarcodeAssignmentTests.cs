using JayTom.Dws.Application.Packages;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Domain.Manager;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证条码与包裹会话的时间窗口匹配和并发安全性。</summary>
public sealed class PackageBarcodeAssignmentTests : IDisposable
{
    /// <summary>测试使用的赋值窗口下界，可按用例覆盖而非依赖生产常量。</summary>
    private const int DefaultMinimumAssignmentMilliseconds = 150;

    /// <summary>测试使用的赋值窗口上界，可按用例覆盖而非依赖生产常量。</summary>
    private const int DefaultMaximumAssignmentMilliseconds = 500;

    /// <summary>用于验证全局包裹会话操作的应用层存储。</summary>
    private readonly PackageSessionStore _store = new();

    /// <summary>每个用例开始前清空全局包裹会话。</summary>
    public PackageBarcodeAssignmentTests()
    {
        _store.ClearAllPackages();
    }

    /// <summary>确认下位机序号索引在添加和移除之间保持一致。</summary>
    [Fact]
    public void PackageIdIndex_TracksActivePackageLifetime()
    {
        var package = new PackageInfo
        {
            Guid = 987654,
            CreateTime = DateTime.Now
        };
        Assert.True(_store.TryAddPackage(package, []));

        Assert.Same(package, _store.GetPackageById(package.Guid));
        Assert.True(_store.RemovePackage(package.CreateTime, "test"));
        Assert.Null(_store.GetPackageById(package.Guid));
    }

    /// <summary>确认重复下位机序号不会产生两个可被回调混淆的运行包裹。</summary>
    [Fact]
    public void DuplicatePackageId_IsRejected()
    {
        var first = new PackageInfo
        {
            Guid = 123456,
            CreateTime = DateTime.Now
        };
        var second = new PackageInfo
        {
            Guid = first.Guid,
            CreateTime = first.CreateTime.AddTicks(1)
        };

        Assert.True(_store.TryAddPackage(first, []));
        Assert.False(_store.TryAddPackage(second, []));
        Assert.Same(first, _store.GetPackageById(first.Guid));
        Assert.Equal(1, _store.GetPackageCount());
    }

    /// <summary>已赋值但尚未移除的包裹不能阻止扫码创建下一包裹。</summary>
    [Fact]
    public void HasUnassignedPackage_IgnoresAlreadyAssignedActivePackage()
    {
        var package = new PackageInfo
        {
            CreateTime = DateTime.Now,
            BarCodeInfo = new BarCodeInfoModel { Barcode = "JT-ASSIGNED" }
        };
        _store.AddPackage(package, []);

        Assert.Equal(1, _store.GetPackageCount());
        Assert.False(_store.HasUnassignedPackage());
    }

    /// <summary>条码与新包裹间隔不足 150ms 时，必须绑定到符合窗口的上一个包裹。</summary>
    [Fact]
    public void TryBindBarcode_SkipsTooYoungPackageAndBindsEligiblePredecessor()
    {
        var firstCreatedAt = new DateTime(2026, 8, 13, 10, 0, 0, DateTimeKind.Local);
        var secondCreatedAt = firstCreatedAt.AddMilliseconds(333);
        var observedAt = firstCreatedAt.AddMilliseconds(335);
        var first = new PackageInfo { CreateTime = firstCreatedAt };
        var second = new PackageInfo { CreateTime = secondCreatedAt };
        _store.AddPackage(first, []);
        _store.AddPackage(second, []);

        var bound = Bind(observedAt, "JT-OLD");

        Assert.Same(first, bound);
        Assert.Equal("JT-OLD", first.BarCodeInfo?.Barcode);
        Assert.Null(second.BarCodeInfo);
    }

    /// <summary>没有任何包裹落入 150ms 至 500ms 窗口时，必须拒绝赋值而不得就近错配。</summary>
    [Theory]
    [InlineData(149)]
    [InlineData(501)]
    public void TryBindBarcode_RejectsWhenNoPackageIsInsideWindow(int observedAgeMilliseconds)
    {
        var createdAt = new DateTime(2026, 8, 13, 10, 0, 0, DateTimeKind.Local);
        var package = new PackageInfo { CreateTime = createdAt };
        _store.AddPackage(package, []);

        var bound = Bind(
            createdAt.AddMilliseconds(observedAgeMilliseconds),
            "JT-OUTSIDE-WINDOW");

        Assert.Null(bound);
        Assert.Null(package.BarCodeInfo);
    }

    /// <summary>赋值边界必须完全服从调用方传入的客户端配置，不能在匹配器内固定为特定毫秒值。</summary>
    [Theory]
    [InlineData(72, false)]
    [InlineData(73, true)]
    [InlineData(287, true)]
    [InlineData(288, false)]
    public void TryBindBarcode_UsesConfiguredAssignmentWindow(
        int observedAgeMilliseconds,
        bool shouldBind)
    {
        var createdAt = new DateTime(2026, 8, 13, 10, 0, 0, DateTimeKind.Local);
        var package = new PackageInfo { CreateTime = createdAt };
        _store.AddPackage(package, []);

        var bound = Bind(
            createdAt.AddMilliseconds(observedAgeMilliseconds),
            "JT-CONFIGURED-WINDOW",
            minimumAssignmentMilliseconds: 73,
            maximumAssignmentMilliseconds: 287);

        Assert.Equal(shouldBind, bound is not null);
    }

    /// <summary>条码在400ms边界到达时，即使定时器尚未执行，也必须以空包裹删除为优先。</summary>
    [Theory]
    [InlineData(399, true)]
    [InlineData(400, false)]
    public void TryBindBarcode_EmptyPackageExpiryHasPriority(
        int observedAgeMilliseconds,
        bool shouldBind)
    {
        var createdAt = new DateTime(2026, 8, 13, 10, 0, 0, DateTimeKind.Local);
        var package = new PackageInfo { CreateTime = createdAt };
        _store.AddPackage(package, []);

        var bound = Bind(
            createdAt.AddMilliseconds(observedAgeMilliseconds),
            "JT-EXPIRY-PRIORITY",
            400);

        Assert.Equal(shouldBind, bound is not null);
        Assert.Equal(shouldBind, package.BarCodeInfo is not null);
    }

    /// <summary>条码虽然及时采集，但处理时已越过空包裹期限时必须拒绝并删除。</summary>
    [Fact]
    public void TryBindBarcode_ProcessingDelayPastExpiryRejectsCapturedBarcode()
    {
        var createdAt = new DateTime(2026, 8, 13, 10, 0, 0, DateTimeKind.Local);
        var package = new PackageInfo { CreateTime = createdAt };
        _store.AddPackage(package, []);

        var bound = Bind(
            createdAt.AddMilliseconds(300),
            "JT-LATE-PROCESSING",
            emptyPackageExpiryMilliseconds: 400,
            processingAt: createdAt.AddMilliseconds(450));

        Assert.Null(bound);
        Assert.Null(_store.GetPackage(createdAt));
    }

    /// <summary>过期队首被删除后，同一个晚到条码不得顺延赋给后续包裹。</summary>
    [Fact]
    public void TryBindBarcode_ExpiredHeadDoesNotSpillIntoFollowingPackage()
    {
        var firstCreatedAt = new DateTime(2026, 8, 13, 10, 0, 0, DateTimeKind.Local);
        var secondCreatedAt = firstCreatedAt.AddMilliseconds(250);
        var first = new PackageInfo { CreateTime = firstCreatedAt };
        var second = new PackageInfo { CreateTime = secondCreatedAt };
        _store.AddPackage(first, []);
        _store.AddPackage(second, []);

        var bound = Bind(
            firstCreatedAt.AddMilliseconds(350),
            "JT-MUST-NOT-SPILL",
            emptyPackageExpiryMilliseconds: 400,
            processingAt: firstCreatedAt.AddMilliseconds(450));

        Assert.Null(bound);
        Assert.Null(second.BarCodeInfo);
    }

    /// <summary>早于包裹创建的条码不得绑定，也不得把尚未到达的包裹误删。</summary>
    [Fact]
    public void TryBindBarcode_ObservedBeforeCreationKeepsFuturePackage()
    {
        var createdAt = new DateTime(2026, 8, 13, 10, 0, 1, DateTimeKind.Local);
        var package = new PackageInfo { CreateTime = createdAt };
        _store.AddPackage(package, []);

        var bound = Bind(
            createdAt.AddMilliseconds(-10),
            "JT-EARLY-OBSERVATION",
            emptyPackageExpiryMilliseconds: 400,
            processingAt: createdAt.AddMilliseconds(10));

        Assert.Null(bound);
        Assert.Same(package, _store.GetPackage(createdAt));
    }

    /// <summary>过期已触发但尚在关键队列排队时，已赋值包裹不得被旧的空包裹判定删除。</summary>
    [Fact]
    public async Task Expiration_RevalidatesEmptyPredicateAfterQueuedBarcodeAssignment()
    {
        var createdAt = new DateTime(2026, 8, 13, 10, 0, 0, DateTimeKind.Local);
        var package = new PackageInfo { CreateTime = createdAt };
        var expirationQueued = new TaskCompletionSource<Action>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _store.AddPackage(package,
        [
            new PackageRemoveTimer
            {
                Description = "空包裹过期",
                RemovalTimeSpan = TimeSpan.FromMilliseconds(10),
                Predicate = pair => pair.Value.BarCodeInfo is null,
                TryDispatch = removal =>
                {
                    expirationQueued.TrySetResult(removal);
                    return true;
                }
            }
        ]);
        var queuedExpiration = await expirationQueued.Task.WaitAsync(
            TimeSpan.FromSeconds(2));

        var bound = Bind(createdAt.AddMilliseconds(200), "JT-RACE-SAFE");
        Assert.Same(package, bound);
        queuedExpiration();

        Assert.Same(package, _store.GetPackage(createdAt));
        Assert.Equal("JT-RACE-SAFE", package.BarCodeInfo?.Barcode);
    }

    /// <summary>确认延迟加入会话的包裹按机械创建时刻到期，而不是重新获得完整生命周期。</summary>
    [Fact]
    public async Task Expiration_SubtractsDelayBeforeSessionRegistration()
    {
        var package = new PackageInfo
        {
            CreateTime = DateTime.Now.AddMilliseconds(-300)
        };
        var expired = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _store.AddPackage(package,
        [
            new PackageRemoveTimer
            {
                Description = "test-expiry",
                RemovalTimeSpan = TimeSpan.FromMilliseconds(400),
                Predicate = pair => pair.Value.BarCodeInfo is null,
                TryDispatch = removal =>
                {
                    removal();
                    expired.TrySetResult();
                    return true;
                }
            }
        ]);

        await expired.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Null(_store.GetPackage(package.CreateTime));
    }

    /// <summary>无锁候选扫描在并发调用下仍只能成功赋值一次，不得把后到条码覆盖到同一包裹。</summary>
    [Fact]
    public void TryBindBarcode_ConcurrentAssignmentsBindPackageOnlyOnce()
    {
        var createdAt = new DateTime(2026, 8, 13, 10, 0, 0, DateTimeKind.Local);
        var package = new PackageInfo { CreateTime = createdAt };
        _store.AddPackage(package, []);
        var observedAt = new DateTime(
            createdAt.Ticks + 200L * TimeSpan.TicksPerMillisecond,
            createdAt.Kind);
        var results = new PackageInfo?[64];

        Parallel.For(0, results.Length, index =>
            results[index] = Bind(observedAt, $"JT-CONCURRENT-{index}"));

        var successfulAssignments = 0;
        foreach (var result in results)
        {
            if (result is not null)
            {
                successfulAssignments++;
            }
        }
        Assert.Equal(1, successfulAssignments);
        Assert.NotNull(package.BarCodeInfo);
    }

    /// <summary>清理测试会话及定时器。</summary>
    public void Dispose()
    {
        _store.ClearAllPackages();
    }

    /// <summary>使用生产配置的 150ms 至 500ms 窗口尝试绑定条码。</summary>
    private PackageInfo? Bind(
        DateTime observedAt,
        string barcode,
        int? emptyPackageExpiryMilliseconds = null,
        int minimumAssignmentMilliseconds = DefaultMinimumAssignmentMilliseconds,
        int maximumAssignmentMilliseconds = DefaultMaximumAssignmentMilliseconds,
        DateTime? processingAt = null) =>
        _store.TryBindBarcode(
            observedAt,
            BarcodeQueueOrderEnum.TimeAscending,
            true,
            minimumAssignmentMilliseconds,
            maximumAssignmentMilliseconds,
            emptyPackageExpiryMilliseconds,
            processingAt ?? observedAt,
            package => package.BarCodeInfo = new BarCodeInfoModel
            {
                Barcode = barcode,
                ScanTime = observedAt,
                BindTime = DateTime.Now
            });
}
