using JayTom.Dws.Legacy.Contracts.Repositories;

namespace JayTom.Dws.Application.CameraConfigurations;

/// <summary>
/// 使用配置仓储实现相机与录像机配置用例。
/// </summary>
/// <typeparam name="TConfiguration">配置实体类型。</typeparam>
public sealed class CameraConfigurationCatalog<TConfiguration>
    : ICameraConfigurationCatalog<TConfiguration>
    where TConfiguration : class {
    /// <summary>持有配置持久化边界。</summary>
    private readonly IRepository<TConfiguration> _repository;

    /// <summary>持有可选的配置缓存边界。</summary>
    private readonly IMemoryCacheRepository<TConfiguration>? _cachedRepository;

    /// <summary>创建相机配置目录。</summary>
    public CameraConfigurationCatalog(IRepository<TConfiguration> repository) {
        _repository = repository;
        _cachedRepository = repository as IMemoryCacheRepository<TConfiguration>;
    }

    /// <summary>读取缓存中的配置。</summary>
    public Task<List<TConfiguration>> ListCachedAsync(CancellationToken cancellationToken = default) =>
        (_cachedRepository ?? throw new InvalidOperationException(
            $"配置类型 {typeof(TConfiguration).Name} 不支持缓存读取。"))
        .MemoryCacheData(cancellationToken);

    /// <summary>按条件和顺序读取持久化配置。</summary>
    public async Task<List<TConfiguration>> ListAsync<TOrder>(
        Func<TConfiguration, bool> predicate,
        Func<TConfiguration, TOrder> orderBy,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(orderBy);
        List<TConfiguration> items = await ReadAllAsync(cancellationToken).ConfigureAwait(false);
        return items.Where(predicate).OrderBy(orderBy).ToList();
    }

    /// <summary>分页读取满足条件的持久化配置。</summary>
    public async Task<List<TConfiguration>> ListPageAsync(
        Func<TConfiguration, bool> predicate,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(predicate);
        int safePageIndex = Math.Max(0, pageIndex);
        int safePageSize = Math.Clamp(pageSize, 1, 1000);
        List<TConfiguration> items = await ReadAllAsync(cancellationToken).ConfigureAwait(false);
        return items.Where(predicate)
            .Skip(safePageIndex * safePageSize)
            .Take(safePageSize)
            .ToList();
    }

    /// <summary>读取首个满足条件的配置。</summary>
    public async Task<TConfiguration?> FindAsync(
        Func<TConfiguration, bool> predicate,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(predicate);
        List<TConfiguration> items = await ReadAllAsync(cancellationToken).ConfigureAwait(false);
        return items.FirstOrDefault(predicate);
    }

    /// <summary>新增配置。</summary>
    public Task<bool> AddAsync(
        TConfiguration configuration,
        CancellationToken cancellationToken = default) =>
        _repository.Insert(configuration, cancellationToken);

    /// <summary>批量新增配置。</summary>
    public Task<bool> AddManyAsync(
        IReadOnlyCollection<TConfiguration> configurations,
        CancellationToken cancellationToken = default) =>
        _repository.InsertRange([.. configurations], cancellationToken);

    /// <summary>新增或更新配置。</summary>
    public Task<bool> SaveAsync(
        TConfiguration configuration,
        CancellationToken cancellationToken = default) =>
        _repository.InsertOrUpdate(configuration, cancellationToken);

    /// <summary>批量新增或更新配置。</summary>
    public Task<bool> SaveManyAsync(
        IReadOnlyCollection<TConfiguration> configurations,
        CancellationToken cancellationToken = default) =>
        _repository.InsertOrUpdateRange([.. configurations], cancellationToken);

    /// <summary>更新配置。</summary>
    public Task<bool> UpdateAsync(
        TConfiguration configuration,
        CancellationToken cancellationToken = default) =>
        _repository.Update(configuration, cancellationToken);

    /// <summary>批量更新配置。</summary>
    public Task<bool> UpdateManyAsync(
        IReadOnlyCollection<TConfiguration> configurations,
        CancellationToken cancellationToken = default) =>
        _repository.UpdateRange([.. configurations], cancellationToken);

    /// <summary>删除配置。</summary>
    public Task<bool> DeleteAsync(
        TConfiguration configuration,
        CancellationToken cancellationToken = default) =>
        _repository.Delete(configuration, cancellationToken);

    /// <summary>批量删除配置。</summary>
    public Task<bool> DeleteManyAsync(
        IReadOnlyCollection<TConfiguration> configurations,
        CancellationToken cancellationToken = default) =>
        _repository.DeleteRange([.. configurations], cancellationToken);

    /// <summary>使后续读取重新加载缓存。</summary>
    public Task RefreshCacheAsync(CancellationToken cancellationToken = default) =>
        _cachedRepository?.UpdateMemoryCache(cancellationToken) ?? Task.CompletedTask;

    /// <summary>在适配器内部读取完整持久化快照，避免将表达式树暴露给应用调用方。</summary>
    private Task<List<TConfiguration>> ReadAllAsync(CancellationToken cancellationToken) =>
        _repository.Select(static _ => true, static _ => 0, cancellationToken);
}
