using JayTom.Dws.Domain.Manager;
using JayTom.Dws.Domain.Dto;

namespace JayTom.Dws.Application.Packages;

/// <summary>
/// 通过应用层边界访问现有线程安全包裹会话，并隔离遗留静态入口。
/// </summary>
public sealed class PackageSessionStore : IPackageSessionStore {
    /// <summary>转发包裹移除事件。</summary>
    public event EventHandler<PackageRemovedEventArgs>? PackageRemoved {
        add => PackageInfoManager.PackageRemoved += value;
        remove => PackageInfoManager.PackageRemoved -= value;
    }

    /// <summary>转发包裹完成事件。</summary>
    public event EventHandler<PackageCompletedEventArgs>? PackageCompleted {
        add => PackageInfoManager.PackageCompleted += value;
        remove => PackageInfoManager.PackageCompleted -= value;
    }

    /// <summary>添加包裹会话。</summary>
    public void AddPackage(PackageInfo package, List<PackageTimer> timers) =>
        PackageInfoManager.AddPackage(package, timers);

    /// <summary>尝试添加包裹会话，避免添加后再次扫描集合确认。</summary>
    public bool TryAddPackage(PackageInfo package, List<PackageTimer> timers) =>
        PackageInfoManager.TryAddPackage(package, timers);

    /// <summary>按创建时间移除包裹会话。</summary>
    public bool RemovePackage(DateTime createTime, string description = "手动移除") =>
        PackageInfoManager.RemovePackage(createTime, description);

    /// <summary>按条件移除包裹会话。</summary>
    public bool RemovePackage(
        Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate,
        string description = "手动移除") =>
        PackageInfoManager.RemovePackage(predicate, description);

    /// <summary>检查包裹会话是否存在。</summary>
    public bool PackageExists(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) =>
        PackageInfoManager.PackageExists(predicate);

    /// <summary>获取最早匹配的包裹会话。</summary>
    public PackageInfo? GetPackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) =>
        PackageInfoManager.GetPackage(predicate);

    /// <summary>获取最后匹配的包裹会话。</summary>
    public PackageInfo? GetLastPackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) =>
        PackageInfoManager.GetLastPackage(predicate);

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
        PackageInfoManager.TryBindBarcode(
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
        PackageInfoManager.GetPackage(createTime);

    /// <summary>按下位机包裹序号直接获取运行会话。</summary>
    public PackageInfo? GetPackageById(long packageId) =>
        PackageInfoManager.GetPackageById(packageId);

    /// <summary>获取匹配的包裹会话快照。</summary>
    public List<PackageInfo> GetPackages(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) =>
        PackageInfoManager.GetPackages(predicate);

    /// <summary>获取当前包裹会话数量。</summary>
    public int GetPackageCount() => PackageInfoManager.GetPackageCount();

    /// <summary>判断是否存在尚未赋值的运行包裹。</summary>
    public bool HasUnassignedPackage() => PackageInfoManager.HasUnassignedPackage();

    /// <summary>清理全部包裹会话。</summary>
    public void ClearAllPackages() => PackageInfoManager.ClearAllPackages();

    /// <summary>完成首个匹配的包裹会话。</summary>
    public void CompletePackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) =>
        PackageInfoManager.CompletedPackage(predicate);

    /// <summary>按创建时间直接完成包裹会话。</summary>
    public void CompletePackage(DateTime createTime) =>
        PackageInfoManager.CompletedPackage(createTime);
}
