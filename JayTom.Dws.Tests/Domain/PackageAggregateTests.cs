using JayTom.Dws.Application.Packages;
using JayTom.Dws.Legacy.Contracts.Packages;
using JayTom.Dws.Domain.Packages;
using JayTom.Dws.Tests.TestDoubles;

namespace JayTom.Dws.Tests.Domain;

/// <summary>验证包裹聚合的值对象、不变量、状态机、事件和只读快照。</summary>
public sealed class PackageAggregateTests
{
    /// <summary>值对象拒绝空条码和负数测量值。</summary>
    [Fact]
    public void Value_objects_enforce_measurement_invariants()
    {
        Assert.Throws<ArgumentException>(() => new PackageBarcode("  "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PackageWeight(-0.01m));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PackageDimensions(1m, -1m, 1m));

        var dimensions = new PackageDimensions(20m, 10m, 5m);
        Assert.Equal(1000m, dimensions.VolumeCubicMillimeters);
        Assert.Equal("JT-001", new PackageBarcode(" JT-001 ").Value);
    }

    /// <summary>生命周期只能通过聚合方法迁移并产生不可变事件。</summary>
    [Fact]
    public void Lifecycle_transitions_are_explicit_and_emit_domain_events()
    {
        var clock = new FixedTimeProvider(
            new DateTimeOffset(2026, 8, 14, 2, 0, 0, TimeSpan.Zero));
        var package = new PackageInfo(clock) { Id = 42 };

        Assert.True(package.TryAssignBarcode(
            new PackageBarcode(" JT-STATE "),
            clock.GetLocalNow().DateTime,
            clock.GetLocalNow().DateTime));
        Assert.False(package.TryAssignBarcode(
            new PackageBarcode("JT-DUPLICATE"),
            clock.GetLocalNow().DateTime,
            clock.GetLocalNow().DateTime));

        package.MarkSortingInstructionExpected();
        package.MarkCompleted();
        package.MarkSortingInstructionSent();

        Assert.Equal(PackageLifecycleState.Completed, package.LifecycleState);
        Assert.IsType<PackageBarcodeAssigned>(package.DomainEvents[0]);
        Assert.Contains(package.DomainEvents, item =>
            item is PackageLifecycleChanged changed &&
            changed.Current == PackageLifecycleState.Completed);

        var capturedEvents = package.DomainEvents;
        package.ClearDomainEvents();
        Assert.NotEmpty(capturedEvents);
        Assert.Empty(package.DomainEvents);
    }

    /// <summary>会话读取公开值快照，快照集合和字段均不能反向修改活动聚合。</summary>
    [Fact]
    public void Session_snapshot_does_not_expose_mutable_aggregate_instances()
    {
        var store = new PackageSessionStore();
        var package = new PackageInfo { Id = 7 };
        store.AddPackage(package, []);

        IReadOnlyList<PackageSessionSnapshot> snapshot = store.GetSnapshot();
        Assert.Collection(snapshot, item => Assert.Equal(7, item.PackageId));
        Assert.False(snapshot[0].HasBarcode);
        Assert.False(snapshot is List<PackageSessionSnapshot>);

        package.TryAssignBarcode(
            new PackageBarcode("JT-SNAPSHOT"),
            package.CreateTime,
            package.CreateTime);
        Assert.False(snapshot[0].HasBarcode);
        Assert.True(store.GetSnapshot()[0].HasBarcode);
        store.ClearAllPackages();
    }

    /// <summary>离开注册表后状态不可逆，聚合不再接受新的条码。</summary>
    [Fact]
    public void Removed_package_rejects_further_assignment()
    {
        var store = new PackageSessionStore();
        var package = new PackageInfo { Id = 8 };
        store.AddPackage(package, []);

        Assert.True(store.RemovePackage(package.CreateTime));
        Assert.Equal(PackageLifecycleState.Removed, package.LifecycleState);
        Assert.False(package.TryAssignBarcode(
            new PackageBarcode("JT-TOO-LATE"),
            package.CreateTime,
            package.CreateTime));
    }
}
