namespace JayTom.Dws.Domain.Repository {

    public interface IMemoryCacheRepository<T> : IRepository<T> where T : class {

        /// <summary>
        /// 获取缓存内容
        /// </summary>
        /// <returns></returns>
        Task<List<T>> MemoryCacheData();
    }
}