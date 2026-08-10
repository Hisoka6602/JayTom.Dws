using System.Linq.Expressions;

namespace JayTom.Dws.Domain.Repository;

/// <summary>
/// 定义改变持久化状态的实体写入能力。
/// </summary>
/// <typeparam name="T">实体类型。</typeparam>
public interface IWriteRepository<T> where T : class {
    /// <summary>插入单个实体。</summary>
    Task<bool> Insert(T entity, CancellationToken cancellationToken = default);

    /// <summary>批量插入实体。</summary>
    Task<bool> InsertRange(List<T> entities, CancellationToken cancellationToken = default);

    /// <summary>插入或更新单个实体。</summary>
    Task<bool> InsertOrUpdate(T entity, CancellationToken cancellationToken = default);

    /// <summary>批量插入或更新实体。</summary>
    Task<bool> InsertOrUpdateRange(
        List<T> entities,
        CancellationToken cancellationToken = default);

    /// <summary>排除指定字段后插入或更新单个实体。</summary>
    Task<bool> InsertOrUpdate(
        T entity,
        Expression<Func<T, object>> excludeColumns,
        CancellationToken cancellationToken = default);

    /// <summary>排除指定字段后批量插入或更新实体。</summary>
    Task<bool> InsertOrUpdateRange(
        List<T> entities,
        Expression<Func<T, object>> excludeColumns,
        CancellationToken cancellationToken = default);

    /// <summary>更新单个实体。</summary>
    Task<bool> Update(T entity, CancellationToken cancellationToken = default);

    /// <summary>批量更新实体。</summary>
    Task<bool> UpdateRange(List<T> entities, CancellationToken cancellationToken = default);

    /// <summary>排除指定字段后批量更新实体。</summary>
    Task<bool> UpdateRange(
        List<T> entities,
        Expression<Func<T, object>> excludeColumns,
        CancellationToken cancellationToken = default);

    /// <summary>删除单个实体。</summary>
    Task<bool> Delete(T entity, CancellationToken cancellationToken = default);

    /// <summary>批量删除实体。</summary>
    Task<bool> DeleteRange(List<T> entities, CancellationToken cancellationToken = default);

    /// <summary>删除指定数量的实体。</summary>
    Task<int> DeleteCount(int count, CancellationToken cancellationToken = default);

    /// <summary>删除指定数量且满足条件的实体。</summary>
    Task<int> DeleteCount(
        int count,
        Expression<Func<T, bool>> where,
        CancellationToken cancellationToken = default);

    /// <summary>使持久化内容与给定实体集合保持同步。</summary>
    Task<bool> SyncEntities(List<T> entities, CancellationToken cancellationToken = default);
}
