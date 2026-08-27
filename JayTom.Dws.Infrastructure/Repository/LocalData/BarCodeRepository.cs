using System.Linq.Expressions;
using JayTom.Dws.Models.Package;
using JayTom.Dws.Models.LocalConf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalData;

namespace JayTom.Dws.Infrastructure.Repository.LocalData {

    public class BarCodeRepository : LocalRepositoryBase<BarCodeInfoModel, SqliteContext>, IBarCodeRepository {

        public BarCodeRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}