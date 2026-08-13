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
        int maximumAssignmentMilliseconds = DefaultMaximumAssignmentMilliseconds) =>
        _store.TryBindBarcode(
            observedAt,
            BarcodeQueueOrderEnum.TimeAscending,
            true,
            minimumAssignmentMilliseconds,
            maximumAssignmentMilliseconds,
            emptyPackageExpiryMilliseconds,
            package => package.BarCodeInfo = new BarCodeInfoModel
            {
                Barcode = barcode,
                ScanTime = observedAt,
                BindTime = DateTime.Now
            });
}
