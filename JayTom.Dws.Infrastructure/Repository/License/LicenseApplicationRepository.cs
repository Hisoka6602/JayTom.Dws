using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.License;
using System.Linq.Expressions;
using System.Collections.Generic;
using JayTom.Dws.Models.VideoApiData;
using JayTom.Dws.Legacy.Contracts.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Legacy.Contracts.Repositories.License;
using JayTom.Dws.Legacy.Contracts.Repositories.VideoApiData;

namespace JayTom.Dws.Infrastructure.Repository.License {

    public class LicenseApplicationRepository :
        MemoryCacheRepositoryBase<LicenseApplicationInfo, LicenseApiContext>, ILicenseApplicationRepository {

        public LicenseApplicationRepository(IDbContextFactory<LicenseApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<KeyValuePair<bool, object>> FirstDetails(Expression<Func<LicenseApplicationInfo, bool>> where, CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<LicenseApplicationInfo>();
                if (dbSet is null) return new KeyValuePair<bool, object>(false, "查询失败");
                dbSet.AsNoTracking();
                var applicationInfo = await dbSet.Where(where)
                    .Include(b => b.LicenseFeatureInfos)
                    .FirstOrDefaultAsync(cancellationToken: token);
                return new KeyValuePair<bool, object>(true, applicationInfo);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, object>(false, "查询失败");
            }
        }

        public async Task<KeyValuePair<bool, object>> Details(Expression<Func<LicenseApplicationInfo, bool>> where, CancellationToken token) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<LicenseApplicationInfo>();
                if (dbSet is null) return new KeyValuePair<bool, object>(false, "查询失败");
                dbSet.AsNoTracking();
                var licenseApplicationInfos = await dbSet.Where(where)
                    .Include(b => b.LicenseFeatureInfos)
                    .ToListAsync(cancellationToken: token);
                return new KeyValuePair<bool, object>(true, licenseApplicationInfos);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, object>(false, "查询失败");
            }
        }
    }
}