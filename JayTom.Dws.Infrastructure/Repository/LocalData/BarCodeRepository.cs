using JayTom.Dws.Data.LocalConf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.LocalConf;

namespace JayTom.Dws.Infrastructure.Repository.LocalData {

    internal class BarCodeRepository : LocalRepositoryBase<ConfigInfoModel>, IConfigRepository {

        public BarCodeRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}