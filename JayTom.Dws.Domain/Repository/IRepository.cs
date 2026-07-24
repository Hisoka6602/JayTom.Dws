using System.Linq.Expressions;

namespace JayTom.Dws.Domain.Repository {

    public interface IRepository<T> where T : class {

        /// <summary>
        /// 执行Sql
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<int> ExecuteSqlAsync(string sql, CancellationToken token = default);

        /// <summary>
        /// sql查询实体
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<List<T>> FromSqlRaw(string sql, CancellationToken token = default);

        /// <summary>
        /// 插入
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> Insert(T entity, CancellationToken token = default);

        /// <summary>
        /// 插入
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="token"></param>
        Task InsertAsync(T entity, CancellationToken token = default);

        /// <summary>
        /// 批量插入
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> InsertRange(List<T> entities, CancellationToken token = default);

        /// <summary>
        /// 插入或者更新一条
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> InsertOrUpdate(T entity, CancellationToken token = default);

        /// <summary>
        /// 插入或者更新批量
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> InsertOrUpdateRange(List<T> entities,
            CancellationToken token = default);

        /// <summary>
        /// 插入或者更新一条
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="excludeColumns"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> InsertOrUpdate(T entity,
            Expression<Func<T, object>> excludeColumns, CancellationToken token = default);

        /// <summary>
        /// 插入或者更新批量
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="excludeColumns"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> InsertOrUpdateRange(List<T> entities,
            Expression<Func<T, object>> excludeColumns,
            CancellationToken token = default);

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> Update(T entity, CancellationToken token = default);

        /// <summary>
        /// 批量更新
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> UpdateRange(List<T> entities, CancellationToken token = default);

        /// <summary>
        /// 批量更新
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="excludeColumns"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> UpdateRange(List<T> entities,
            Expression<Func<T, object>> excludeColumns,
            CancellationToken token = default);

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> Delete(T entity, CancellationToken token = default);

        /// <summary>
        /// 批量删除
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> DeleteRange(List<T> entities, CancellationToken token = default);

        /// <summary>
        /// 删除指定条数(无条件)
        /// </summary>
        /// <param name="count"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<int> DeleteCount(int count, CancellationToken token = default);

        /// <summary>
        /// 删除指定条数(带条件)
        /// </summary>
        /// <param name="count"></param>
        /// <param name="where"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<int> DeleteCount(int count, Expression<Func<T, bool>> @where, CancellationToken token = default);

        /// <summary>
        /// 查询列表(无排序)
        /// </summary>
        /// <returns></returns>
        Task<List<T>> Select(Expression<Func<T, bool>> @where, int pageIndex, int pageSize, CancellationToken token = default);

        /// <summary>
        /// 查询列表
        /// </summary>
        /// <typeparam name="TOrder"></typeparam>
        /// <param name="where"></param>
        /// <param name="order"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<List<T>> Select<TOrder>(Expression<Func<T, bool>> @where, Expression<Func<T, TOrder>> order, int pageIndex, int pageSize, CancellationToken token = default);

        /// <summary>
        /// 查询列表
        /// </summary>
        /// <typeparam name="TOrder"></typeparam>
        /// <param name="where"></param>
        /// <param name="order"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<List<T>> Select<TOrder>(Expression<Func<T, bool>> @where, Expression<Func<T, TOrder>> order, CancellationToken token = default);

        /// <summary>
        /// 查询列表(倒序)
        /// </summary>
        /// <typeparam name="TOrder"></typeparam>
        /// <param name="where"></param>
        /// <param name="order"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<List<T>> SelectOrderByDescending<TOrder>(Expression<Func<T, bool>> @where,
            Expression<Func<T, TOrder>> order, int pageIndex, int pageSize, CancellationToken token = default);

        /// <summary>
        /// 查询列表(倒序)
        /// </summary>
        /// <typeparam name="TOrder"></typeparam>
        /// <param name="where"></param>
        /// <param name="order"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<List<T>> SelectOrderByDescending<TOrder>(Expression<Func<T, bool>> @where,
            Expression<Func<T, TOrder>> order, CancellationToken token = default);

        /// <summary>
        /// 首个符合条件实体对象
        /// </summary>
        /// <param name="where"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<T?> FirstOrDefault(Expression<Func<T, bool>> @where, CancellationToken token = default);

        /// <summary>
        /// 总数
        /// </summary>
        /// <param name="where"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<int> Total(Expression<Func<T, bool>> @where, CancellationToken token = default);

        /// <summary>
        /// 同步实体
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> SyncEntities(List<T> entities, CancellationToken token = default);
    }
}
