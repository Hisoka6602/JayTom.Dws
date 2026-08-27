using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Application.PackageExits;

/// <summary>
/// 提供出口锁绑定的应用层管理边界。
/// </summary>
public interface IPackageExitLockBindingCatalog {
    /// <summary>读取全部出口锁绑定。</summary>
    Task<IReadOnlyList<PackageExitLockBindingInfoModel>> ListAsync(
        CancellationToken cancellationToken = default);

    /// <summary>判断指定出口是否已经绑定。</summary>
    Task<bool> ExistsForExitAsync(long exitId, CancellationToken cancellationToken = default);

    /// <summary>新增出口锁绑定。</summary>
    Task<bool> AddAsync(
        PackageExitLockBindingInfoModel binding,
        CancellationToken cancellationToken = default);

    /// <summary>更新出口锁绑定。</summary>
    Task<bool> UpdateAsync(
        PackageExitLockBindingInfoModel binding,
        CancellationToken cancellationToken = default);

    /// <summary>批量保存出口锁绑定。</summary>
    Task<bool> SaveRangeAsync(
        IReadOnlyCollection<PackageExitLockBindingInfoModel> bindings,
        CancellationToken cancellationToken = default);

    /// <summary>按出口标识删除绑定。</summary>
    Task<bool> DeleteByExitAsync(long exitId, CancellationToken cancellationToken = default);

    /// <summary>使持久化出口锁绑定与给定快照保持同步。</summary>
    Task<bool> SyncAsync(
        IReadOnlyCollection<PackageExitLockBindingInfoModel> bindings,
        CancellationToken cancellationToken = default);
}
