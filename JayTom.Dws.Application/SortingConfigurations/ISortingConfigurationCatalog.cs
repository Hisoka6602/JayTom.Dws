using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Application.SortingConfigurations;

/// <summary>
/// 提供带规则明细的分拣配置管理用例。
/// </summary>
/// <typeparam name="TConfiguration">分拣配置实体类型。</typeparam>
public interface ISortingConfigurationCatalog<TConfiguration>
    where TConfiguration : BasePackageSortingConfig {
    /// <summary>读取全部配置及其规则明细。</summary>
    Task<IReadOnlyList<TConfiguration>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>新增配置及其规则明细。</summary>
    Task<bool> AddAsync(TConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>更新配置及其规则明细。</summary>
    Task<bool> UpdateAsync(TConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>批量新增配置及其规则明细。</summary>
    Task<bool> AddRangeAsync(
        IReadOnlyCollection<TConfiguration> configurations,
        CancellationToken cancellationToken = default);

    /// <summary>按标识删除一项配置。</summary>
    Task<bool> DeleteByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>按标识集合批量删除配置。</summary>
    Task<bool> DeleteByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default);

    /// <summary>删除当前类型的全部配置。</summary>
    Task<bool> DeleteAllAsync(CancellationToken cancellationToken = default);

    /// <summary>使持久化配置与给定快照保持同步。</summary>
    Task<bool> SyncAsync(
        IReadOnlyCollection<TConfiguration> configurations,
        CancellationToken cancellationToken = default);
}
