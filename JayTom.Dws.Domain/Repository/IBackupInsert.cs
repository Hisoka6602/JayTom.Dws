using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Domain.Repository {

    public interface IBackupInsert<T> where T : class {

        /// <summary>
        /// 备份式插入
        /// </summary>
        /// <param name="dataRepository"></param>
        /// <param name="insertSlim"></param>
        /// <param name="entity"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> Insert(IRepository<T> dataRepository,
            SemaphoreSlim insertSlim, T entity, CancellationToken token);

        /// <summary>
        /// 备份式插入(集合)
        /// </summary>
        /// <param name="dataRepository"></param>
        /// <param name="insertSlim"></param>
        /// <param name="entities"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> InsertRange(IRepository<T> dataRepository,
            SemaphoreSlim insertSlim, List<T> entities, CancellationToken token);

        /// <summary>
        /// 备份式(更新或插入)
        /// </summary>
        /// <param name="dataRepository"></param>
        /// <param name="insertSlim"></param>
        /// <param name="entity"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> InsertOrUpdate(IRepository<T> dataRepository,
            SemaphoreSlim insertSlim, T entity, CancellationToken token);

        /// <summary>
        /// 备份式(更新或批量)
        /// </summary>
        /// <param name="dataRepository"></param>
        /// <param name="insertSlim"></param>
        /// <param name="entities"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> InsertOrUpdateRange(IRepository<T> dataRepository,
            SemaphoreSlim insertSlim, List<T> entities,
            CancellationToken token);
    }
}
