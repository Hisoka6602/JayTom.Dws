using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Application.PackageExits;

/// <summary>
/// 使用出口锁仓储实现绑定管理用例。
/// </summary>
public sealed class PackageExitLockBindingCatalog : IPackageExitLockBindingCatalog {
    /// <summary>持有出口锁绑定的持久化边界。</summary>
    private readonly IPackageExitLockBindingRepository _repository;

    /// <summary>创建出口锁绑定目录。</summary>
    public PackageExitLockBindingCatalog(IPackageExitLockBindingRepository repository) {
        _repository = repository;
    }

    /// <summary>读取全部出口锁绑定。</summary>
    public async Task<IReadOnlyList<PackageExitLockBindingInfoModel>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await _repository.Select(item => item.Id > 0, item => item.Id, cancellationToken)
            .ConfigureAwait(false);

    /// <summary>判断指定出口是否已经绑定。</summary>
    public async Task<bool> ExistsForExitAsync(long exitId, CancellationToken cancellationToken = default) =>
        await _repository.FirstOrDefault(item => item.ExitId == exitId, cancellationToken)
            .ConfigureAwait(false) is not null;

    /// <summary>新增出口锁绑定。</summary>
    public Task<bool> AddAsync(
        PackageExitLockBindingInfoModel binding,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(binding);
        return _repository.Insert(binding, cancellationToken);
    }

    /// <summary>更新出口锁绑定。</summary>
    public Task<bool> UpdateAsync(
        PackageExitLockBindingInfoModel binding,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(binding);
        return _repository.Update(binding, cancellationToken);
    }

    /// <summary>批量保存出口锁绑定。</summary>
    public Task<bool> SaveRangeAsync(
        IReadOnlyCollection<PackageExitLockBindingInfoModel> bindings,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(bindings);
        return _repository.InsertOrUpdateRange([.. bindings], cancellationToken);
    }

    /// <summary>按出口标识删除绑定。</summary>
    public async Task<bool> DeleteByExitAsync(long exitId, CancellationToken cancellationToken = default) {
        var binding = await _repository.FirstOrDefault(item => item.ExitId == exitId, cancellationToken)
            .ConfigureAwait(false);
        return binding is not null &&
               await _repository.Delete(binding, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>使持久化出口锁绑定与给定快照保持同步。</summary>
    public Task<bool> SyncAsync(
        IReadOnlyCollection<PackageExitLockBindingInfoModel> bindings,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(bindings);
        return _repository.SyncEntities([.. bindings], cancellationToken);
    }
}
