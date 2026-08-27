namespace JayTom.Dws.Application.CameraConfigurations;

/// <summary>
/// 在相机页面迁移期间把旧配置操作映射到应用层目录语义。
/// </summary>
public static class CameraConfigurationCatalogCompatibilityExtensions {
    /// <summary>读取缓存配置。</summary>
    public static Task<List<TConfiguration>> MemoryCacheData<TConfiguration>(
        this ICameraConfigurationCatalog<TConfiguration> catalog)
        where TConfiguration : class => catalog.ListCachedAsync();

    /// <summary>按条件和顺序读取配置。</summary>
    public static Task<List<TConfiguration>> Select<TConfiguration, TOrder>(
        this ICameraConfigurationCatalog<TConfiguration> catalog,
        Func<TConfiguration, bool> predicate,
        Func<TConfiguration, TOrder> orderBy,
        CancellationToken cancellationToken = default)
        where TConfiguration : class =>
        catalog.ListAsync(predicate, orderBy, cancellationToken);

    /// <summary>分页读取满足条件的配置。</summary>
    public static Task<List<TConfiguration>> Select<TConfiguration>(
        this ICameraConfigurationCatalog<TConfiguration> catalog,
        Func<TConfiguration, bool> predicate,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
        where TConfiguration : class =>
        catalog.ListPageAsync(predicate, pageIndex, pageSize, cancellationToken);

    /// <summary>读取首个满足条件的配置。</summary>
    public static Task<TConfiguration?> FirstOrDefault<TConfiguration>(
        this ICameraConfigurationCatalog<TConfiguration> catalog,
        Func<TConfiguration, bool> predicate,
        CancellationToken cancellationToken = default)
        where TConfiguration : class =>
        catalog.FindAsync(predicate, cancellationToken);

    /// <summary>新增配置。</summary>
    public static Task<bool> Insert<TConfiguration>(
        this ICameraConfigurationCatalog<TConfiguration> catalog,
        TConfiguration configuration)
        where TConfiguration : class => catalog.AddAsync(configuration);

    /// <summary>批量新增配置。</summary>
    public static Task<bool> InsertRange<TConfiguration>(
        this ICameraConfigurationCatalog<TConfiguration> catalog,
        IReadOnlyCollection<TConfiguration> configurations)
        where TConfiguration : class => catalog.AddManyAsync(configurations);

    /// <summary>新增或更新配置。</summary>
    public static Task<bool> InsertOrUpdate<TConfiguration>(
        this ICameraConfigurationCatalog<TConfiguration> catalog,
        TConfiguration configuration)
        where TConfiguration : class => catalog.SaveAsync(configuration);

    /// <summary>批量新增或更新配置。</summary>
    public static Task<bool> InsertOrUpdateRange<TConfiguration>(
        this ICameraConfigurationCatalog<TConfiguration> catalog,
        IReadOnlyCollection<TConfiguration> configurations)
        where TConfiguration : class => catalog.SaveManyAsync(configurations);

    /// <summary>更新配置。</summary>
    public static Task<bool> Update<TConfiguration>(
        this ICameraConfigurationCatalog<TConfiguration> catalog,
        TConfiguration configuration)
        where TConfiguration : class => catalog.UpdateAsync(configuration);

    /// <summary>批量更新配置。</summary>
    public static Task<bool> UpdateRange<TConfiguration>(
        this ICameraConfigurationCatalog<TConfiguration> catalog,
        IReadOnlyCollection<TConfiguration> configurations)
        where TConfiguration : class => catalog.UpdateManyAsync(configurations);

    /// <summary>删除配置。</summary>
    public static Task<bool> Delete<TConfiguration>(
        this ICameraConfigurationCatalog<TConfiguration> catalog,
        TConfiguration configuration)
        where TConfiguration : class => catalog.DeleteAsync(configuration);

    /// <summary>批量删除配置。</summary>
    public static Task<bool> DeleteRange<TConfiguration>(
        this ICameraConfigurationCatalog<TConfiguration> catalog,
        IReadOnlyCollection<TConfiguration> configurations)
        where TConfiguration : class => catalog.DeleteManyAsync(configurations);

    /// <summary>使后续读取重新加载缓存。</summary>
    public static void UpdateMemoryCache<TConfiguration>(
        this ICameraConfigurationCatalog<TConfiguration> catalog)
        where TConfiguration : class =>
        JayTom.Dws.Abstractions.Threading.TaskCleanup.Observe(catalog.RefreshCacheAsync());
}
