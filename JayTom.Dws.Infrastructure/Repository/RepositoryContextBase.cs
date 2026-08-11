using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JayTom.Dws.Infrastructure.Repository;

/// <summary>
/// 集中保存所有 EF 仓储共享的上下文工厂与缓存依赖。
/// </summary>
/// <typeparam name="TContext">仓储使用的数据库上下文类型。</typeparam>
public abstract class RepositoryContextBase<TContext>
    where TContext : DbContext {
    /// <summary>
    /// 获取用于按操作创建短生命周期上下文的工厂。
    /// </summary>
    protected readonly IDbContextFactory<TContext> _contextFactory;

    /// <summary>
    /// 获取仓储可选使用的进程内缓存。
    /// </summary>
    protected readonly IMemoryCache _cache;

    /// <summary>
    /// 初始化仓储共享依赖并执行空值校验。
    /// </summary>
    /// <param name="contextFactory">数据库上下文工厂。</param>
    /// <param name="cache">进程内缓存。</param>
    protected RepositoryContextBase(
        IDbContextFactory<TContext> contextFactory,
        IMemoryCache cache) {
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentNullException.ThrowIfNull(cache);
        _contextFactory = contextFactory;
        _cache = cache;
    }
}
