using JayTom.Dws.Application.Packages;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Legacy.Contracts.Packages;
using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证同次扫码索引在添加、赋值和所有移除路径中保持一致。</summary>
public sealed class PackageBarcodeObservationIndexTests : IDisposable
{
    private readonly PackageSessionStore _store = new();
    private static readonly DateTime Origin = new(2026, 10, 3, 10, 0, 0, DateTimeKind.Local);

    /// <summary>同一时刻的不同相机具有独立观测索引。</summary>
    [Fact]
    public void Binding_indexes_scan_time_and_camera_without_conflating_cameras()
    {
        var first = Add(1, Origin);
        var second = Add(2, Origin.AddMilliseconds(300));
        var observedAt = Origin.AddMilliseconds(650);

        Bind(observedAt, "camera-1");
        Bind(observedAt, "camera-2");

        Assert.Same(first, _store.GetPackageByBarcodeObservation(observedAt, "camera-1"));
        Assert.Same(second, _store.GetPackageByBarcodeObservation(observedAt, "camera-2"));
        Assert.Null(_store.GetPackageByBarcodeObservation(observedAt.AddTicks(1), "camera-1"));
    }

    /// <summary>按时间和按条件移除都会清理相机观测索引。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Both_removal_paths_remove_observation_indexes(bool usePredicate)
    {
        var package = Add(1, Origin);
        var observedAt = Origin.AddMilliseconds(650);
        Bind(observedAt, "camera-1");

        Assert.True(usePredicate
            ? _store.RemovePackage(pair => pair.Key == Origin)
            : _store.RemovePackage(package.CreateTime));

        Assert.Null(_store.GetPackageByBarcodeObservation(observedAt, "camera-1"));
        Assert.False(_store.HasUnassignedPackage());
    }

    /// <summary>入队时已经有条码的包裹也支持直接查找并在清空时释放索引。</summary>
    [Fact]
    public void Already_assigned_packages_are_indexed_on_add_and_removed_on_clear()
    {
        var package = new PackageInfo
        {
            Id = 1,
            CreateTime = Origin,
            BarCodeInfo = Barcode(Origin.AddMilliseconds(650), "camera-1")
        };
        _store.AddPackage(package, []);
        Assert.Same(package, _store.GetPackageByBarcodeObservation(package.BarCodeInfo.ScanTime, "camera-1"));

        _store.ClearAllPackages();

        Assert.Null(_store.GetPackageByBarcodeObservation(package.BarCodeInfo.ScanTime, "camera-1"));
    }

    /// <summary>多条码模式复用观测键时，旧会话移除不能破坏新会话。</summary>
    [Fact]
    public void Removing_an_older_package_does_not_delete_a_reused_observation_key()
    {
        var older = Add(1, Origin);
        var newer = Add(2, Origin.AddMilliseconds(300));
        var observedAt = Origin.AddMilliseconds(650);
        Bind(observedAt, "camera-1");
        Bind(observedAt, "camera-1");

        Assert.True(_store.RemovePackage(older.CreateTime));
        Assert.Same(newer, _store.GetPackageByBarcodeObservation(observedAt, "camera-1"));
    }

    private PackageInfo Add(long id, DateTime createdAt)
    {
        var package = new PackageInfo { Id = id, CreateTime = createdAt };
        _store.AddPackage(package, []);
        return package;
    }

    private void Bind(DateTime observedAt, string serialNumber) =>
        Assert.NotNull(_store.TryBindBarcode(observedAt, BarcodeQueueOrderEnum.TimeAscending,
            false, 0, 0, null, observedAt,
            package => package.BarCodeInfo = Barcode(observedAt, serialNumber)));

    private static BarCodeInfoModel Barcode(DateTime observedAt, string serialNumber) => new()
    {
        Barcode = "JT-INDEX",
        SerialNumber = serialNumber,
        ScanTime = observedAt,
        BindTime = observedAt
    };

    /// <summary>清理每个测试实例的会话和资源。</summary>
    public void Dispose() => _store.ClearAllPackages();
}
