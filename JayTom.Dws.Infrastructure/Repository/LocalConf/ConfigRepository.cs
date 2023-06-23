using JayTom.Dws.Data.LocalConf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.LocalConf;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf {

    public class ConfigRepository : LocalRepositoryBase<ConfigInfoModel>, IConfigRepository {

        public ConfigRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}