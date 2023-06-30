using JayTom.Dws.Data.LocalConf;
using JayTom.Dws.Data.LocalData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;

namespace JayTom.Dws.Infrastructure.Repository.LocalData {

    public class BarCodeRepository : LocalRepositoryBase<BarCodeInfoModel>, IBarCodeRepository {

        public BarCodeRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}