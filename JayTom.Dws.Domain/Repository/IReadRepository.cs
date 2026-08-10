using System.Linq.Expressions;

namespace JayTom.Dws.Domain.Repository;

/// <summary>
/// 定义不改变持久化状态的实体查询能力。
/// </summary>
/// <typeparam name="T">实体类型。</typeparam>
public interface IReadRepository<T> where T : class {
    /// <summary>分页查询满足条件的实体。</summary>
    Task<List<T>> Select(
        Expression<Func<T, bool>> where,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>分页并按指定字段正序查询实体。</summary>
    Task<List<T>> Select<TOrder>(
        Expression<Func<T, bool>> where,
        Expression<Func<T, TOrder>> order,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>按指定字段正序查询全部满足条件的实体。</summary>
    Task<List<T>> Select<TOrder>(
        Expression<Func<T, bool>> where,
        Expression<Func<T, TOrder>> order,
        CancellationToken cancellationToken = default);

    /// <summary>分页并按指定字段倒序查询实体。</summary>
    Task<List<T>> SelectOrderByDescending<TOrder>(
        Expression<Func<T, bool>> where,
        Expression<Func<T, TOrder>> order,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>按指定字段倒序查询全部满足条件的实体。</summary>
    Task<List<T>> SelectOrderByDescending<TOrder>(
        Expression<Func<T, bool>> where,
        Expression<Func<T, TOrder>> order,
        CancellationToken cancellationToken = default);

    /// <summary>获取首个满足条件的实体。</summary>
    Task<T?> FirstOrDefault(
        Expression<Func<T, bool>> where,
        CancellationToken cancellationToken = default);

    /// <summary>统计满足条件的实体数量。</summary>
    Task<int> Total(
        Expression<Func<T, bool>> where,
        CancellationToken cancellationToken = default);
}
