using JayTom.Dws.Application.Packages;
using JayTom.Dws.Models.Package;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Legacy.Contracts.Packages;
using JayTom.Dws.Tests.TestDoubles;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证条码与包裹会话的时间窗口匹配和并发安全性。</summary>
public sealed class PackageBarcodeAssignmentTests : IDisposable
{
    /// <summary>验证包裹创建时间来自可替换业务时钟。</summary>
    [Fact]
    public void Package_creation_uses_injected_time_provider()
    {
        var clock = new FixedTimeProvider(
            new DateTimeOffset(2026, 8, 14, 1, 2, 3, TimeSpan.Zero));

        var package = new PackageInfo(clock);

        Assert.Equal(clock.GetLocalNow().DateTime, package.CreateTime);
    }

    /// <summary>测试使用的赋值窗口下界，可按用例覆盖而非依赖生产常量。</summary>
    private const int DefaultMinimumAssignmentMilliseconds = 150;

    /// <summary>测试使用的赋值窗口上界，可按用例覆盖而非依赖生产常量。</summary>
    private const int DefaultMaximumAssignmentMilliseconds = 500;

    /// <summary>用于验证单个应用实例包裹会话操作的应用层存储。</summary>
    private readonly PackageSessionStore _store = new();

    /// <summary>每个用例开始前清空当前测试实例的包裹会话。</summary>
    public PackageBarcodeAssignmentTests()
    {
        _store.ClearAllPackages();
    }

    /// <summary>不同应用实例的会话存储必须彼此隔离，禁止退化为进程全局静态状态。</summary>
    [Fact]
    public void PackageSessionStores_OwnIndependentRegistries()
    {
        var firstStore = new PackageSessionStore();
        var secondStore = new PackageSessionStore();
        var package = new PackageInfo { CreateTime = DateTime.Now };

        firstStore.AddPackage(package, []);

        Assert.Equal(1, firstStore.GetPackageCount());
        Assert.Equal(0, secondStore.GetPackageCount());
        Assert.Null(secondStore.GetPackage(package.CreateTime));
        firstStore.ClearAllPackages();
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

    /// <summary>倒序模式必须跳过未达到最小间隔的新包裹，并绑定窗口内最新的成熟包裹。</summary>
    [Fact]
    public void TryBindBarcode_DescendingOrderSkipsPackageBelowConfiguredMinimum()
    {
        var firstCreatedAt = new DateTime(2026, 8, 13, 10, 0, 0, DateTimeKind.Local);
        var secondCreatedAt = firstCreatedAt.AddMilliseconds(200);
        var observedAt = firstCreatedAt.AddMilliseconds(250);
        var first = new PackageInfo { CreateTime = firstCreatedAt };
        var second = new PackageInfo { CreateTime = secondCreatedAt };
        _store.AddPackage(first, []);
        _store.AddPackage(second, []);

        var bound = _store.TryBindBarcode(
            observedAt,
            BarcodeQueueOrderEnum.TimeDescending,
            true,
            150,
            500,
            null,
            observedAt,
            package => package.BarCodeInfo = new BarCodeInfoModel
            {
                Barcode = "JT-DESCENDING",
                ScanTime = observedAt,
                BindTime = observedAt
            });

        Assert.Same(first, bound);
        Assert.Null(second.BarCodeInfo);
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

    /// <summary>区间匹配应跳过过期队首，继续选择区间内仍可赋值的包裹。</summary>
    [Fact]
    public void TryBindBarcode_WindowSkipsExpiredHeadAndBindsEligibleSuccessor()
    {
        var firstCreatedAt = new DateTime(2026, 8, 13, 10, 0, 0, DateTimeKind.Local);
        var secondCreatedAt = firstCreatedAt.AddMilliseconds(250);
        var first = new PackageInfo { CreateTime = firstCreatedAt };
        var second = new PackageInfo { CreateTime = secondCreatedAt };
        _store.AddPackage(first, []);
        _store.AddPackage(second, []);
        var observedAt = firstCreatedAt.AddMilliseconds(450);

        var bound = _store.TryBindBarcode(
            observedAt,
            BarcodeQueueOrderEnum.TimeDescending,
            true,
            150,
            500,
            400,
            observedAt,
            package => package.BarCodeInfo = new BarCodeInfoModel
            {
                Barcode = "JT-MUST-NOT-SPILL-DESC",
                ScanTime = observedAt,
                BindTime = observedAt
            });

        Assert.Same(second, bound);
        Assert.Null(first.BarCodeInfo);
        Assert.NotNull(second.BarCodeInfo);
    }

    /// <summary>20、60、500ms案例先过滤50至700ms区间；正序先选500ms，倒序先选60ms，区间为空才拒绝。</summary>
    [Theory]
    [InlineData(BarcodeQueueOrderEnum.TimeAscending, 500L, 60L)]
    [InlineData(BarcodeQueueOrderEnum.TimeDescending, 60L, 500L)]
    public void TryBindBarcode_WindowUsesConfiguredOrder(
        BarcodeQueueOrderEnum queueOrder, long expectedFirstId, long expectedSecondId)
    {
        var observedAt = new DateTime(2026, 10, 3, 11, 30, 0, DateTimeKind.Local);
        var twenty = new PackageInfo { CreateTime = observedAt.AddMilliseconds(-20), Guid = 20 };
        var sixty = new PackageInfo { CreateTime = observedAt.AddMilliseconds(-60), Guid = 60 };
        var fiveHundred = new PackageInfo { CreateTime = observedAt.AddMilliseconds(-500), Guid = 500 };
        var tooOld = new PackageInfo { CreateTime = observedAt.AddMilliseconds(-800), Guid = 800 };
        var future = new PackageInfo { CreateTime = observedAt.AddMilliseconds(10), Guid = 10 };
        foreach (var package in new[] { twenty, tooOld, fiveHundred, future, sixty }) _store.AddPackage(package, []);

        PackageInfo? Assign(string barcode) => _store.TryBindBarcode(observedAt, queueOrder,
            true, 50, 700, null, observedAt,
            package => package.BarCodeInfo = new BarCodeInfoModel { Barcode = barcode });

        Assert.Equal(expectedFirstId, Assign("JT-FIRST")?.Guid);
        Assert.Equal(expectedSecondId, Assign("JT-SECOND")?.Guid);
        Assert.Null(Assign("JT-NONE-IN-WINDOW"));
        Assert.Null(twenty.BarCodeInfo);
        Assert.Null(tooOld.BarCodeInfo);
        Assert.Null(future.BarCodeInfo);
    }

    /// <summary>赋值窗口两端包含边界，不能把边界内包裹误判为不匹配。</summary>
    [Theory]
    [InlineData(49, false)]
    [InlineData(50, true)]
    [InlineData(700, true)]
    [InlineData(701, false)]
    public void TryBindBarcode_FiftyToSevenHundredIncludesEndpoints(int interval, bool expected)
    {
        var observedAt = new DateTime(2026, 10, 3, 11, 30, 0, DateTimeKind.Local);
        var package = new PackageInfo { CreateTime = observedAt.AddMilliseconds(-interval) };
        _store.AddPackage(package, []);
        var result = _store.TryBindBarcode(observedAt, BarcodeQueueOrderEnum.TimeAscending,
            true, 50, 700, null, observedAt,
            target => target.BarCodeInfo = new BarCodeInfoModel { Barcode = "JT-BOUNDARY" });
        Assert.Equal(expected, result is not null);
    }

    /// <summary>窗口内最近包裹已被赋值时，应继续匹配下一个未赋值候选。</summary>
    [Fact]
    public void TryBindBarcode_WindowSkipsAlreadyAssignedCandidate()
    {
        var observedAt = new DateTime(2026, 10, 3, 11, 30, 0, DateTimeKind.Local);
        var assigned = new PackageInfo { CreateTime = observedAt.AddMilliseconds(-60),
            BarCodeInfo = new BarCodeInfoModel { Barcode = "JT-EXISTING" } };
        var available = new PackageInfo { CreateTime = observedAt.AddMilliseconds(-500) };
        _store.AddPackage(assigned, []);
        _store.AddPackage(available, []);
        var result = Bind(observedAt, "JT-NEXT", minimumAssignmentMilliseconds: 50,
            maximumAssignmentMilliseconds: 700);
        Assert.Same(available, result);
        Assert.Equal("JT-EXISTING", assigned.BarCodeInfo?.Barcode);
    }

    /// <summary>关闭赋值区间后保留配置的时间顺序选择方式。</summary>
    [Theory]
    [InlineData(BarcodeQueueOrderEnum.TimeAscending, 500L)]
    [InlineData(BarcodeQueueOrderEnum.TimeDescending, 20L)]
    public void TryBindBarcode_DisabledWindowKeepsConfiguredOrder(BarcodeQueueOrderEnum queueOrder, long expectedId)
    {
        var observedAt = new DateTime(2026, 10, 3, 11, 30, 0, DateTimeKind.Local);
        foreach (var interval in new[] { 20, 60, 500 })
            _store.AddPackage(new PackageInfo { CreateTime = observedAt.AddMilliseconds(-interval), Guid = interval }, []);
        var result = _store.TryBindBarcode(observedAt, queueOrder, false, 50, 700, null, observedAt,
            target => target.BarCodeInfo = new BarCodeInfoModel { Barcode = "JT-ORDER" });
        Assert.Equal(expectedId, result?.Guid);
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

    /// <summary>需要 API 格口指令的包裹必须一直保留到全部指令实际写入物理连接。</summary>
    [Fact]
    public void SortingInstructionLifecycle_RemainsPendingUntilPhysicalSendCompletes()
    {
        var package = new PackageInfo();

        package.MarkSortingInstructionExpected();

        Assert.True(package.IsWaitingForSortingInstruction);

        package.MarkSortingInstructionSent();

        Assert.False(package.IsWaitingForSortingInstruction);
    }

    /// <summary>清理测试创建的全局包裹会话和计时资源。</summary>
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
