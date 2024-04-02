using Newtonsoft.Json;
using JayTom.Dws.Data.LocalConf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.LocalConf;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf {

    public class ConfigRepository : LocalRepositoryBase<ConfigInfoModel>, IConfigRepository {

        public ConfigRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<T?> FirstOrDefaultEntity<T>(string keyName, CancellationToken token) where T : class {
            try {
                var configInfoModel = await base.FirstOrDefault(f => f.ConfigName.Equals(keyName), token);
                if (configInfoModel is not null) {
                    return JsonConvert.DeserializeObject<T>(configInfoModel.Value);
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"配置项Json反序列化错误:{e}");
            }

            return null;
        }
    }
}