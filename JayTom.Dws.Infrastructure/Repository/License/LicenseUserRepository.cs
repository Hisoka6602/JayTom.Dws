using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.License;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.License;

namespace JayTom.Dws.Infrastructure.Repository.License {

    public class LicenseUserRepository :
        MemoryCacheRepositoryBase<LicenseUserInfo>, ILicenseUserRepository {

        public LicenseUserRepository(IDbContextFactory<LicenseApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<KeyValuePair<bool, object>> DetailsInfo(string userCode, CancellationToken token) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<LicenseUserInfo>();
                if (dbSet is null) return new KeyValuePair<bool, object>(false, "查询失败");
                dbSet.AsNoTracking();
                var licenseUserInfo = await dbSet.Where(w => w.UserCode.Equals(userCode))
                    .Include(b => b.UserDetailsInfo)
                    .FirstOrDefaultAsync(cancellationToken: token);
                return new KeyValuePair<bool, object>(true, licenseUserInfo);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, object>(false, "查询失败");
            }
        }
    }
}