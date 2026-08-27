namespace JayTom.Dws.Legacy.Contracts.Repositories {

    public interface IMemoryCacheRepository<T> : IRepository<T> where T : class {

        /// <summary>
        /// 获取缓存内容
        /// </summary>
        /// <returns></returns>
        Task<List<T>> MemoryCacheData(CancellationToken cancellationToken = default);

        /// <summary>
        /// 手动更新缓存
        /// </summary>
        /// <returns></returns>
        Task UpdateMemoryCache(CancellationToken cancellationToken = default);
    }
}
