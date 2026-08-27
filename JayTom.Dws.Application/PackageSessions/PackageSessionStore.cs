using JayTom.Dws.Legacy.Contracts.Packages;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Domain.Packages;

namespace JayTom.Dws.Application.Packages;

/// <summary>
/// 通过应用层边界访问进程作用域的线程安全包裹会话。
/// </summary>
public sealed class PackageSessionStore : IPackageSessionStore {
    /// <summary>当前应用实例独占的包裹会话注册表。</summary>
    private readonly PackageSessionRegistry _registry = new();

    /// <summary>转发包裹移除事件。</summary>
    public event EventHandler<PackageRemovedEventArgs>? PackageRemoved {
        add => _registry.PackageRemoved += value;
        remove => _registry.PackageRemoved -= value;
    }

    /// <summary>转发包裹完成事件。</summary>
    public event EventHandler<PackageCompletedEventArgs>? PackageCompleted {
        add => _registry.PackageCompleted += value;
        remove => _registry.PackageCompleted -= value;
    }

    /// <summary>添加包裹会话。</summary>
    public void AddPackage(PackageInfo package, List<PackageTimer> timers) =>
        _registry.AddPackage(package, timers);

    /// <summary>尝试添加包裹会话，避免添加后再次扫描集合确认。</summary>
    public bool TryAddPackage(PackageInfo package, List<PackageTimer> timers) =>
        _registry.TryAddPackage(package, timers);

    /// <summary>按创建时间移除包裹会话。</summary>
    public bool RemovePackage(DateTime createTime, string description = "手动移除") =>
        _registry.RemovePackage(createTime, description);

    /// <summary>按条件移除包裹会话。</summary>
    public bool RemovePackage(
        Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate,
        string description = "手动移除") =>
        _registry.RemovePackage(predicate, description);

    /// <summary>检查包裹会话是否存在。</summary>
    public bool PackageExists(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) =>
        _registry.PackageExists(predicate);

    /// <summary>获取最早匹配的包裹会话。</summary>
    public PackageInfo? GetPackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) =>
        _registry.GetPackage(predicate);

    /// <summary>获取最后匹配的包裹会话。</summary>
    public PackageInfo? GetLastPackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) =>
        _registry.GetLastPackage(predicate);

    /// <summary>按观测时间原子绑定条码到符合条件的包裹会话。</summary>
    public PackageInfo? TryBindBarcode(
        DateTime observedAt,
        BarcodeQueueOrderEnum queueOrder,
        bool enforceAssignmentInterval,
        int minimumAssignmentMilliseconds,
        int maximumAssignmentMilliseconds,
        int? emptyPackageExpiryMilliseconds,
        DateTime processingAt,
        Action<PackageInfo> assignment) =>
        _registry.TryBindBarcode(
            observedAt,
            queueOrder,
            enforceAssignmentInterval,
            minimumAssignmentMilliseconds,
            maximumAssignmentMilliseconds,
            emptyPackageExpiryMilliseconds,
            processingAt,
            assignment);

    /// <summary>按创建时间获取包裹会话。</summary>
    public PackageInfo? GetPackage(DateTime createTime) =>
        _registry.GetPackage(createTime);

    /// <summary>按下位机包裹序号直接获取运行会话。</summary>
    public PackageInfo? GetPackageById(long packageId) =>
        _registry.GetPackageById(packageId);

    /// <summary>获取匹配的包裹会话快照。</summary>
    public List<PackageInfo> GetPackages(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) =>
        _registry.GetPackages(predicate);

    /// <summary>获取不泄漏可变聚合实例的活动会话快照。</summary>
    public IReadOnlyList<PackageSessionSnapshot> GetSnapshot() => _registry.GetSnapshot();

    /// <summary>获取当前包裹会话数量。</summary>
    public int GetPackageCount() => _registry.GetPackageCount();

    /// <summary>判断是否存在尚未赋值的运行包裹。</summary>
    public bool HasUnassignedPackage() => _registry.HasUnassignedPackage();

    /// <summary>清理全部包裹会话。</summary>
    public void ClearAllPackages() => _registry.ClearAllPackages();

    /// <summary>完成首个匹配的包裹会话。</summary>
    public void CompletePackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) =>
        _registry.CompletedPackage(predicate);

    /// <summary>按创建时间直接完成包裹会话。</summary>
    public void CompletePackage(DateTime createTime) =>
        _registry.CompletedPackage(createTime);
}
