using Microsoft.EntityFrameworkCore;

namespace JayTom.Dws.Tests.TestDoubles;

/// <summary>使用委托创建数据库上下文的轻量测试工厂。</summary>
/// <typeparam name="TContext">数据库上下文类型。</typeparam>
internal sealed class DelegateDbContextFactory<TContext> : IDbContextFactory<TContext>
    where TContext : DbContext
{
    /// <summary>上下文创建委托。</summary>
    private readonly Func<TContext> _factory;

    /// <summary>创建委托数据库上下文工厂。</summary>
    public DelegateDbContextFactory(Func<TContext> factory) =>
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));

    /// <summary>同步创建上下文。</summary>
    public TContext CreateDbContext() => _factory();

    /// <summary>在尊重取消请求后创建上下文。</summary>
    public Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_factory());
    }
}
