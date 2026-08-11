using JayTom.Dws.Domain.Manager;

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

    /// <summary>按创建时间获取包裹会话。</summary>
    public PackageInfo? GetPackage(DateTime createTime) =>
        PackageInfoManager.GetPackage(createTime);

    /// <summary>获取匹配的包裹会话快照。</summary>
    public List<PackageInfo> GetPackages(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) =>
        PackageInfoManager.GetPackages(predicate);

    /// <summary>获取当前包裹会话数量。</summary>
    public int GetPackageCount() => PackageInfoManager.GetPackageCount();

    /// <summary>清理全部包裹会话。</summary>
    public void ClearAllPackages() => PackageInfoManager.ClearAllPackages();

    /// <summary>完成首个匹配的包裹会话。</summary>
    public void CompletePackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) =>
        PackageInfoManager.CompletedPackage(predicate);
}
