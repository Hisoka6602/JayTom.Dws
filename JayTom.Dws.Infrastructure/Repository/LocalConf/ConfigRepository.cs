using System.Linq;
using Newtonsoft.Json;
using JayTom.Dws.Models.LocalConf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf {

    public class ConfigRepository : MemoryCacheRepositoryBase<ConfigInfoModel, SqliteConfContext>, IConfigRepository {

        public ConfigRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<T?> FirstOrDefaultEntity<T>(string keyName, CancellationToken token) where T : class {
            try {
                var configInfoModels = await base.MemoryCacheData(token);
                var configInfoModel = configInfoModels.FirstOrDefault(f => f.ConfigName.Equals(keyName));
                if (configInfoModel is not null) {
                    return JsonConvert.DeserializeObject<T>(configInfoModel.Value);
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"配置项Json反序列化错误:{e}");
            }

            return null;
        }

        public async Task<string> FirstOrDefaultJsonEntity(string keyName, CancellationToken token = default) {
            try {
                var configInfoModels = await base.MemoryCacheData(token);
                var configInfoModel = configInfoModels.FirstOrDefault(f => f.ConfigName.Equals(keyName));
                if (configInfoModel is not null) {
                    return configInfoModel.Value;
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($":{e}");
            }

            return string.Empty;
        }
    }
}
