using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Application.SortingConfigurations;

/// <summary>
/// 使用统一明细仓储实现分拣配置管理用例。
/// </summary>
/// <typeparam name="TConfiguration">分拣配置实体类型。</typeparam>
public sealed class SortingConfigurationCatalog<TConfiguration> : ISortingConfigurationCatalog<TConfiguration>
    where TConfiguration : BasePackageSortingConfig {
    /// <summary>持有当前分拣配置类型的持久化边界。</summary>
    private readonly ISortingDetailRepository<TConfiguration> _repository;

    /// <summary>创建分拣配置目录。</summary>
    public SortingConfigurationCatalog(ISortingDetailRepository<TConfiguration> repository) {
        _repository = repository;
    }

    /// <summary>读取全部配置及其规则明细。</summary>
    public async Task<IReadOnlyList<TConfiguration>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await _repository.SelectDetails(item => item.Id > 0, cancellationToken).ConfigureAwait(false);

    /// <summary>新增配置及其规则明细。</summary>
    public Task<bool> AddAsync(
        TConfiguration configuration,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(configuration);
        return _repository.InsertDetailAsync(configuration, cancellationToken);
    }

    /// <summary>更新配置及其规则明细。</summary>
    public Task<bool> UpdateAsync(
        TConfiguration configuration,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(configuration);
        return _repository.UpdateDetailAsync(configuration, cancellationToken);
    }

    /// <summary>批量新增配置及其规则明细。</summary>
    public Task<bool> AddRangeAsync(
        IReadOnlyCollection<TConfiguration> configurations,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(configurations);
        return _repository.InsertRangeDetailAsync([.. configurations], cancellationToken);
    }

    /// <summary>按标识删除一项配置。</summary>
    public async Task<bool> DeleteByIdAsync(long id, CancellationToken cancellationToken = default) {
        var entity = await _repository.FirstOrDefault(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return entity is not null &&
               await _repository.Delete(entity, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>按标识集合批量删除配置。</summary>
    public async Task<bool> DeleteByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0) {
            return true;
        }

        var entities = await _repository.Select(item => ids.Contains(item.Id), item => item.Id, cancellationToken)
            .ConfigureAwait(false);
        return entities.Count == 0 ||
               await _repository.DeleteRange(entities, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>删除当前类型的全部配置。</summary>
    public async Task<bool> DeleteAllAsync(CancellationToken cancellationToken = default) {
        var entities = await _repository.Select(item => item.Id > 0, item => item.Id, cancellationToken)
            .ConfigureAwait(false);
        return entities.Count == 0 ||
               await _repository.DeleteRange(entities, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>使持久化配置与给定快照保持同步。</summary>
    public Task<bool> SyncAsync(
        IReadOnlyCollection<TConfiguration> configurations,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(configurations);
        return _repository.SyncEntities([.. configurations], cancellationToken);
    }
}
