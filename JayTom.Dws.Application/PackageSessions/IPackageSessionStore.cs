using JayTom.Dws.Domain.Manager;

namespace JayTom.Dws.Application.Packages;

/// <summary>
/// 定义运行期包裹会话的线程安全访问边界。
/// </summary>
public interface IPackageSessionStore {
    /// <summary>在包裹从会话移除后发生。</summary>
    event EventHandler<PackageRemovedEventArgs>? PackageRemoved;

    /// <summary>在包裹完成数据填充后发生。</summary>
    event EventHandler<PackageCompletedEventArgs>? PackageCompleted;

    /// <summary>添加包裹并启动关联计时器。</summary>
    /// <param name="package">待添加的包裹。</param>
    /// <param name="timers">包裹生命周期计时器。</param>
    void AddPackage(PackageInfo package, List<PackageTimer> timers);

    /// <summary>根据创建时间移除包裹。</summary>
    /// <param name="createTime">包裹创建时间。</param>
    /// <param name="description">移除原因。</param>
    /// <returns>是否成功移除。</returns>
    bool RemovePackage(DateTime createTime, string description = "手动移除");

    /// <summary>移除首个符合条件的包裹。</summary>
    /// <param name="predicate">筛选条件。</param>
    /// <param name="description">移除原因。</param>
    /// <returns>是否成功移除。</returns>
    bool RemovePackage(
        Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate,
        string description = "手动移除");

    /// <summary>检查是否存在符合条件的包裹。</summary>
    /// <param name="predicate">筛选条件。</param>
    /// <returns>是否存在符合条件的包裹。</returns>
    bool PackageExists(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate);

    /// <summary>获取最早符合条件的包裹。</summary>
    /// <param name="predicate">筛选条件。</param>
    /// <returns>符合条件的包裹，找不到时返回空。</returns>
    PackageInfo? GetPackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate);

    /// <summary>获取最后一个符合条件的包裹。</summary>
    /// <param name="predicate">筛选条件。</param>
    /// <returns>符合条件的包裹，找不到时返回空。</returns>
    PackageInfo? GetLastPackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate);

    /// <summary>根据创建时间获取包裹。</summary>
    /// <param name="createTime">包裹创建时间。</param>
    /// <returns>匹配的包裹，找不到时返回空。</returns>
    PackageInfo? GetPackage(DateTime createTime);

    /// <summary>获取所有符合条件的包裹快照。</summary>
    /// <param name="predicate">筛选条件。</param>
    /// <returns>包裹快照集合。</returns>
    List<PackageInfo> GetPackages(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate);

    /// <summary>获取当前包裹数量。</summary>
    /// <returns>包裹数量。</returns>
    int GetPackageCount();

    /// <summary>清理全部包裹并释放其资源。</summary>
    void ClearAllPackages();

    /// <summary>将首个符合条件的包裹标记为完成。</summary>
    /// <param name="predicate">筛选条件。</param>
    void CompletePackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate);
}
