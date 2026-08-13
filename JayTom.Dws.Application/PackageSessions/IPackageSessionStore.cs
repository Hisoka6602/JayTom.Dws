using JayTom.Dws.Domain.Manager;
using JayTom.Dws.Domain.Dto;

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

    /// <summary>尝试添加包裹并启动计时器，创建时间重复时直接返回失败。</summary>
    /// <param name="package">待添加的包裹。</param>
    /// <param name="timers">包裹生命周期计时器。</param>
    /// <returns>包裹是否成功进入运行会话。</returns>
    bool TryAddPackage(PackageInfo package, List<PackageTimer> timers);

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

    /// <summary>按设备观测时间原子选择符合条件的未赋值包裹，并执行条码赋值。</summary>
    /// <param name="observedAt">设备或通讯层观测到条码的时间。</param>
    /// <param name="queueOrder">包裹选择顺序。</param>
    /// <param name="enforceAssignmentInterval">是否校验赋值时间窗口。</param>
    /// <param name="minimumAssignmentMilliseconds">赋值时间窗口下限，单位毫秒。</param>
    /// <param name="maximumAssignmentMilliseconds">赋值时间窗口上限，单位毫秒。</param>
    /// <param name="emptyPackageExpiryMilliseconds">空包裹删除时间，到达该时间后删除规则优先。</param>
    /// <param name="assignment">在包裹锁内执行的赋值操作。</param>
    /// <returns>赋值成功的包裹，无合适包裹时返回空。</returns>
    PackageInfo? TryBindBarcode(
        DateTime observedAt,
        BarcodeQueueOrderEnum queueOrder,
        bool enforceAssignmentInterval,
        int minimumAssignmentMilliseconds,
        int maximumAssignmentMilliseconds,
        int? emptyPackageExpiryMilliseconds,
        Action<PackageInfo> assignment);

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

    /// <summary>按创建时间直接完成指定包裹。</summary>
    /// <param name="createTime">包裹创建时间。</param>
    void CompletePackage(DateTime createTime);
}
