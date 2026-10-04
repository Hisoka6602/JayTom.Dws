using JayTom.Dws.Abstractions.Imaging;
using JayTom.Dws.Application.Packages;
using JayTom.Dws.Domain.Packages;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Legacy.Contracts.Packages;
using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证 NoRead 仍消费小车占位，且不完成业务包裹或污染下一辆小车。</summary>
public sealed class PackageNoReadConsumptionTests : IDisposable
{
    private readonly PackageSessionStore _store = new();
    private static readonly DateTime Origin = new(2026, 10, 3, 10, 0, 0, DateTimeKind.Local);

    /// <summary>延迟空读消费空车占位，下一条有效码保留自己的小车编号。</summary>
    [Fact]
    public void NoRead_consumes_empty_cart_and_next_barcode_binds_to_its_own_cart()
    {
        var empty = AddCart(101, Origin);
        var loaded = AddCart(102, Origin.AddMilliseconds(300));
        var completed = 0;
        PackageRemovedEventArgs? removal = null;
        _store.PackageCompleted += (_, _) => completed++;
        _store.PackageRemoved += (_, args) => removal = args;

        // 相机返回超过一个机械周期，期间第二辆小车也已经创建。
        var consumed = Discard(Origin.AddMilliseconds(650), out var rejection);

        Assert.Same(empty, consumed);
        Assert.Null(rejection);
        Assert.Null(empty.BarCodeInfo);
        Assert.False(empty.IsCompleted);
        Assert.Equal(PackageLifecycleState.Removed, empty.LifecycleState);
        Assert.NotNull(removal);
        Assert.True(removal.IsDiscarded);
        Assert.Null(_store.GetPackageById(101));
        Assert.Equal(0, completed);

        var bound = Bind(Origin.AddMilliseconds(950), "JT-102");
        Assert.Same(loaded, bound);
        Assert.Equal(102, bound.Guid);
        _store.CompletePackage(bound.CreateTime);
        Assert.Equal(1, completed);
    }

    /// <summary>空读与有效码遵循相同的正序或倒序匹配规则。</summary>
    [Theory]
    [InlineData(BarcodeQueueOrderEnum.TimeAscending, 101)]
    [InlineData(BarcodeQueueOrderEnum.TimeDescending, 102)]
    public void NoRead_uses_the_same_configured_order_as_barcodes(BarcodeQueueOrderEnum order, long expectedId)
    {
        AddCart(101, Origin);
        AddCart(102, Origin.AddMilliseconds(300));

        var discarded = _store.TryDiscardUnassignedPackage(
            Origin.AddMilliseconds(650), order, false, 0, 0, null,
            Origin.AddMilliseconds(650), out var rejection);

        Assert.NotNull(discarded);
        Assert.Equal(expectedId, discarded.Guid);
        Assert.Null(rejection);
        Assert.Equal(1, _store.GetPackageCount());
    }

    /// <summary>空读消费仍严格服从用户配置的赋值时间窗口。</summary>
    [Theory]
    [InlineData(149, false)]
    [InlineData(150, true)]
    [InlineData(500, true)]
    [InlineData(501, false)]
    public void NoRead_respects_assignment_window_boundaries(int observedAge, bool shouldConsume)
    {
        var cart = AddCart(101, Origin);
        var observedAt = Origin.AddMilliseconds(observedAge);

        var discarded = _store.TryDiscardUnassignedPackage(
            observedAt, BarcodeQueueOrderEnum.TimeAscending, true, 150, 500,
            null, observedAt, out var rejection);

        Assert.Equal(shouldConsume, discarded is not null);
        Assert.Equal(shouldConsume, _store.GetPackageById(101) is null);
        Assert.Equal(shouldConsume, rejection is null);
        if (shouldConsume)
        {
            Assert.Same(cart, discarded);
        }
    }

    /// <summary>过期结果只处理原来的队首，不能顺带消费下一辆小车。</summary>
    [Fact]
    public void Expired_NoRead_does_not_consume_the_following_cart()
    {
        AddCart(101, Origin);
        var following = AddCart(102, Origin.AddMilliseconds(300));
        PackageRemovedEventArgs? removal = null;
        _store.PackageRemoved += (_, args) => removal = args;
        var observedAt = Origin.AddMilliseconds(450);

        var result = _store.TryDiscardUnassignedPackage(
            observedAt, BarcodeQueueOrderEnum.TimeAscending, false, 0, 0,
            400, observedAt, out var rejection);

        Assert.Null(result);
        Assert.NotNull(rejection);
        Assert.Equal(BarcodeAssignmentRejectionReason.EmptyPackageExpiredAtObservation, rejection.Reason);
        Assert.Null(_store.GetPackageById(101));
        Assert.Same(following, _store.GetPackageById(102));
        Assert.Null(following.BarCodeInfo);
        Assert.NotNull(removal);
        Assert.True(removal.IsDiscarded);
    }

    /// <summary>早于创建时刻的结果不能消费未来小车。</summary>
    [Fact]
    public void NoRead_observed_before_creation_leaves_future_cart_available()
    {
        var cart = AddCart(101, Origin);

        var result = Discard(Origin.AddTicks(-1), out var rejection);

        Assert.Null(result);
        Assert.NotNull(rejection);
        Assert.Equal(BarcodeAssignmentRejectionReason.ObservedBeforePackageCreation, rejection.Reason);
        Assert.Same(cart, _store.GetPackageById(101));
    }

    /// <summary>连续空车与有效包裹交错时，不允许小车编号产生持续偏移。</summary>
    [Fact]
    public void Dense_mixed_results_preserve_every_cart_id_without_NoRead_completion()
    {
        const int count = 4096;
        for (var index = 0; index < count; index++)
        {
            AddCart(index + 1, Origin.AddMilliseconds(index * 10));
        }
        var completed = 0;
        var discarded = 0;
        _store.PackageCompleted += (_, _) => completed++;
        _store.PackageRemoved += (_, args) => discarded += args.IsDiscarded ? 1 : 0;
        var observedAt = Origin.AddMinutes(1);

        for (var index = 0; index < count; index++)
        {
            var expectedId = index + 1;
            if (index % 3 != 0)
            {
                var result = Discard(observedAt, out var rejection);
                Assert.NotNull(result);
                Assert.Equal(expectedId, result.Guid);
                Assert.Null(rejection);
            }
            else
            {
                var result = Bind(observedAt, $"JT-{expectedId}");
                Assert.NotNull(result);
                Assert.Equal(expectedId, result.Guid);
                _store.CompletePackage(result.CreateTime);
                Assert.True(_store.RemovePackage(result.CreateTime));
            }
            observedAt = observedAt.AddMilliseconds(10);
        }

        Assert.Equal((count + 2) / 3, completed);
        Assert.Equal(count - completed, discarded);
        Assert.Equal(0, _store.GetPackageCount());
        Assert.False(_store.HasUnassignedPackage());
    }

    /// <summary>同一占位在空读和有效码并发争用时只能被成功消费一次。</summary>
    [Fact]
    public void Concurrent_NoRead_and_barcode_can_consume_a_cart_only_once()
    {
        var cart = AddCart(101, Origin);
        var results = new PackageInfo?[64];
        var observedAt = Origin.AddMilliseconds(650);

        Parallel.For(0, results.Length, index =>
        {
            results[index] = index % 2 == 0
                ? Discard(observedAt, out _)
                : Bind(observedAt, $"JT-{index}");
        });

        Assert.Equal(1, results.Count(result => result is not null));
        Assert.True(cart.BarCodeInfo is not null || cart.LifecycleState == PackageLifecycleState.Removed);
        Assert.False(_store.HasUnassignedPackage());
    }

    /// <summary>内部清理订阅者抛错不能阻止图像释放和索引删除。</summary>
    [Fact]
    public void Discard_releases_owned_image_even_when_an_internal_subscriber_throws()
    {
        var resource = new OwnedResource();
        var cart = AddCart(101, Origin);
        cart.Image = ImageHandle.TakeOwnership(resource);
        _store.PackageRemoved += (_, _) => throw new InvalidOperationException("subscriber-failed");

        Assert.Throws<InvalidOperationException>(() => Discard(Origin.AddMilliseconds(650), out _));

        Assert.Equal(1, resource.DisposeCount);
        Assert.Null(cart.Image);
        Assert.Null(_store.GetPackageById(101));
        Assert.Equal(0, _store.GetPackageCount());
    }

    /// <summary>小车再次循环经过设备时，可以复用已消费的编号。</summary>
    [Fact]
    public void Discarded_cart_id_can_be_reused_without_leaving_stale_indexes()
    {
        AddCart(101, Origin);
        Assert.NotNull(Discard(Origin.AddMilliseconds(650), out _));
        var nextLap = AddCart(101, Origin.AddMilliseconds(900));

        Assert.Same(nextLap, Bind(Origin.AddMilliseconds(1200), "JT-NEXT-LAP"));
        Assert.Same(nextLap, _store.GetPackageById(101));
    }

    private PackageInfo AddCart(long id, DateTime createdAt)
    {
        var cart = new PackageInfo
        {
            Id = id,
            CreateTime = createdAt,
            IsCreatedByLowerMachine = true,
            WeightInfo = new WeightInfoModel(),
            VolumeInfo = new VolumeInfoModel()
        };
        Assert.True(_store.TryAddPackage(cart, []));
        return cart;
    }

    private PackageInfo? Discard(DateTime observedAt, out BarcodeAssignmentRejection? rejection) =>
        _store.TryDiscardUnassignedPackage(
            observedAt, BarcodeQueueOrderEnum.TimeAscending, false,
            0, 0, null, observedAt, out rejection);

    private PackageInfo? Bind(DateTime observedAt, string barcode) =>
        _store.TryBindBarcode(observedAt, BarcodeQueueOrderEnum.TimeAscending,
            false, 0, 0, null, observedAt,
            package => package.BarCodeInfo = new BarCodeInfoModel
            {
                Barcode = barcode,
                SerialNumber = "camera-1",
                ScanTime = observedAt,
                BindTime = observedAt
            });

    /// <summary>释放每个测试拥有的会话和计时器。</summary>
    public void Dispose() => _store.ClearAllPackages();

    private sealed class OwnedResource : IDisposable
    {
        public int DisposeCount { get; private set; }
        public void Dispose() => DisposeCount++;
    }
}
