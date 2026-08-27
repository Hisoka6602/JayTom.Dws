namespace JayTom.Dws.Application.CameraConfigurations;

/// <summary>
/// 提供相机与录像机配置的应用层读写边界。
/// </summary>
/// <typeparam name="TConfiguration">配置实体类型。</typeparam>
public interface ICameraConfigurationCatalog<TConfiguration>
    where TConfiguration : class {
    /// <summary>读取缓存中的配置。</summary>
    Task<List<TConfiguration>> ListCachedAsync(CancellationToken cancellationToken = default);

    /// <summary>按条件和顺序读取持久化配置。</summary>
    Task<List<TConfiguration>> ListAsync<TOrder>(
        Func<TConfiguration, bool> predicate,
        Func<TConfiguration, TOrder> orderBy,
        CancellationToken cancellationToken = default);

    /// <summary>分页读取满足条件的持久化配置。</summary>
    Task<List<TConfiguration>> ListPageAsync(
        Func<TConfiguration, bool> predicate,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>读取首个满足条件的配置。</summary>
    Task<TConfiguration?> FindAsync(
        Func<TConfiguration, bool> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>新增配置。</summary>
    Task<bool> AddAsync(
        TConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>批量新增配置。</summary>
    Task<bool> AddManyAsync(
        IReadOnlyCollection<TConfiguration> configurations,
        CancellationToken cancellationToken = default);

    /// <summary>新增或更新配置。</summary>
    Task<bool> SaveAsync(
        TConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>批量新增或更新配置。</summary>
    Task<bool> SaveManyAsync(
        IReadOnlyCollection<TConfiguration> configurations,
        CancellationToken cancellationToken = default);

    /// <summary>更新配置。</summary>
    Task<bool> UpdateAsync(
        TConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>批量更新配置。</summary>
    Task<bool> UpdateManyAsync(
        IReadOnlyCollection<TConfiguration> configurations,
        CancellationToken cancellationToken = default);

    /// <summary>删除配置。</summary>
    Task<bool> DeleteAsync(
        TConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>批量删除配置。</summary>
    Task<bool> DeleteManyAsync(
        IReadOnlyCollection<TConfiguration> configurations,
        CancellationToken cancellationToken = default);

    /// <summary>使后续读取重新加载缓存。</summary>
    Task RefreshCacheAsync(CancellationToken cancellationToken = default);
}
