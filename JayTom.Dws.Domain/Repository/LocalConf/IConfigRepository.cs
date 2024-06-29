using JayTom.Dws.Data.LocalConf;

namespace JayTom.Dws.Domain.Repository.LocalConf {

    public interface IConfigRepository : IMemoryCacheRepository<ConfigInfoModel> {

        /// <summary>
        /// 读取对象实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keyName"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<T?> FirstOrDefaultEntity<T>(string keyName, CancellationToken token = default) where T : class;

        /// <summary>
        /// 读取对象Json
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<string> FirstOrDefaultJsonEntity(string keyName, CancellationToken token = default);
    }
}