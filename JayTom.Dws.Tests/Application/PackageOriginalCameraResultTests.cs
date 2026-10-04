using JayTom.Dws.Application.Packages;
using JayTom.Dws.Legacy.Contracts.Packages;
using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证时间窗口只校验原小车，拒绝、移除和并发结果都不能转交下一辆车。</summary>
public sealed class PackageOriginalCameraResultTests : IDisposable
{
    private readonly PackageSessionStore _store = new();
    private static readonly DateTime Origin = new(2026, 10, 4, 10, 0, 0);

    /// <summary>窗口外的有效码和空读都关闭原车，下一次结果仍绑定自己的车。</summary>
    [Theory]
    [InlineData(false, 600)]
    [InlineData(true, 600)]
    [InlineData(false, 49)]
    [InlineData(true, 49)]
    [InlineData(false, -1)]
    public void Rejected_original_result_never_binds_or_consumes_following_cart(bool discard, int observedAge)
    {
        var original = Add(101, 0);
        var following = Add(102, 300);
        PackageRemovedEventArgs? removal = null;
        _store.PackageRemoved += (_, args) => removal = args;

        var result = Consume(original, observedAge, discard, out var rejection);

        Assert.Null(result);
        Assert.NotNull(rejection);
        Assert.Equal(original.Timestamp, rejection.PackageTimestamp);
        Assert.Equal(original.Guid, rejection.PackageId);
        Assert.Equal(original.CreateTime, rejection.PackageCreatedAt);
        Assert.Null(_store.GetPackage(original.CreateTime));
        Assert.Null(following.BarCodeInfo);
        Assert.NotNull(removal);
        Assert.True(removal.IsDiscarded);
        Assert.Same(following, Consume(following, 650, false, out _));
        Assert.Equal("JT-102", following.BarCodeInfo?.Barcode);
    }

    /// <summary>原车已由过期回调或手动移除时，只丢弃原车结果。</summary>
    [Fact]
    public void Removed_original_result_cannot_fall_through_to_another_cart_with_the_same_id()
    {
        var original = Add(101, 0);
        _store.RemovePackage(original.CreateTime);
        var nextLap = Add(101, 300);

        Assert.Null(Consume(original, 600, false, out var rejection));
        Assert.Equal(BarcodeAssignmentRejectionReason.OriginalPackageUnavailable, rejection?.Reason);
        Assert.Null(nextLap.BarCodeInfo);
        Assert.Same(nextLap, Consume(nextLap, 650, false, out _));
    }

    /// <summary>关闭窗口时，固定触发归属不受相机和创建指令时间戳差异影响。</summary>
    [Fact]
    public void Fixed_original_without_window_accepts_observation_before_creation_timestamp()
    {
        var original = Add(101, 300);
        var following = Add(102, 600);
        Assert.Same(original, _store.TryConsumeCameraResultForPackage(original.CreateTime,
            Origin.AddMilliseconds(100), false, 50, 500, null, Origin.AddMilliseconds(700),
            Assign(original), out var rejection));
        Assert.Null(rejection);
        Assert.Null(following.BarCodeInfo);
        Assert.Same(following, Consume(following, 850, false, out _));
    }

    /// <summary>处理排队超过空包裹期限时，只关闭已确定的原小车。</summary>
    [Fact]
    public void Processing_expiry_closes_original_without_spilling_into_following_cart()
    {
        var original = Add(101, 0);
        var following = Add(102, 300);

        var result = _store.TryConsumeCameraResultForPackage(original.CreateTime, Origin.AddMilliseconds(200),
            true, 50, 500, 500, Origin.AddMilliseconds(600), Assign(original), out var rejection);

        Assert.Null(result);
        Assert.Equal(BarcodeAssignmentRejectionReason.EmptyPackageExpiredWhileQueued, rejection?.Reason);
        Assert.Null(_store.GetPackage(original.CreateTime));
        Assert.Null(following.BarCodeInfo);
    }

    /// <summary>无效窗口也必须关闭已消费的原包裹，恢复正常配置后继续处理下一辆车。</summary>
    [Fact]
    public void Invalid_window_does_not_leave_consumed_original_pending()
    {
        var original = Add(101, 0);
        var following = Add(102, 300);

        Assert.Null(_store.TryConsumeCameraResultForPackage(original.CreateTime, Origin.AddMilliseconds(200),
            true, 500, 50, null, Origin.AddMilliseconds(200), Assign(original), out var rejection));
        Assert.Equal(BarcodeAssignmentRejectionReason.InvalidAssignmentWindow, rejection?.Reason);
        Assert.Null(_store.GetPackage(original.CreateTime));
        Assert.Same(following, Consume(following, 650, false, out _));
    }

    /// <summary>指定原车的上下窗口边界包含等于值。</summary>
    [Theory]
    [InlineData(50)]
    [InlineData(500)]
    public void Original_window_boundaries_are_inclusive(int observedAge)
    {
        var original = Add(101, 0);
        Assert.Same(original, Consume(original, observedAge, false, out var rejection));
        Assert.Null(rejection);
    }

    /// <summary>同车结果并发重复时只接受一次，其他车的占位完全不受影响。</summary>
    [Fact]
    public async Task Concurrent_results_for_original_have_one_winner_without_consuming_other_carts()
    {
        var original = Add(101, 0);
        var following = Add(102, 300);
        var results = await Task.WhenAll(Enumerable.Range(0, 64).Select(iteration =>
            Task.Run(() => Consume(original, 350, false, out _))));

        Assert.Equal(1, results.Count(package => ReferenceEquals(package, original)));
        Assert.Null(following.BarCodeInfo);
        Assert.Same(following, Consume(following, 650, false, out _));
    }

    private PackageInfo Add(long id, int offset)
    {
        var package = new PackageInfo { Guid = id, CreateTime = Origin.AddMilliseconds(offset), IsCreatedByLowerMachine = true };
        _store.AddPackage(package, []);
        return package;
    }

    private PackageInfo? Consume(PackageInfo package, int observedAge, bool discard, out BarcodeAssignmentRejection? rejection) =>
        _store.TryConsumeCameraResultForPackage(package.CreateTime, Origin.AddMilliseconds(observedAge),
            true, 50, 500, null, Origin.AddMilliseconds(observedAge), discard ? null : Assign(package), out rejection);

    private static Action<PackageInfo> Assign(PackageInfo expected) => package =>
        package.BarCodeInfo = new BarCodeInfoModel { Barcode = $"JT-{expected.Guid}" };

    /// <summary>释放测试中的包裹资源。</summary>
    public void Dispose() => _store.ClearAllPackages();
}
