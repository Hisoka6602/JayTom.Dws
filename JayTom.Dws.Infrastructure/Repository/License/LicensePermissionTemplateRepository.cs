using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.License;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Legacy.Contracts.Repositories.License;

namespace JayTom.Dws.Infrastructure.Repository.License {

    public class LicensePermissionTemplateRepository :
        MemoryCacheRepositoryBase<LicensePermissionTemplateInfo, LicenseApiContext>, ILicensePermissionTemplateRepository {

        public LicensePermissionTemplateRepository(IDbContextFactory<LicenseApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<KeyValuePair<bool, object>> Details(Expression<Func<LicensePermissionTemplateInfo, bool>> where, CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<LicensePermissionTemplateInfo>();
                if (dbSet is null) return new KeyValuePair<bool, object>(false, "查询失败");
                dbSet.AsNoTracking();
                var licensePermissionTemplateInfo = await dbSet.Where(where)
                    .Include(b => b.LicenseApplicationInfo)
                    .ThenInclude(c => c.LicenseFeatureInfos)
                    .ToListAsync(cancellationToken: token);
                return new KeyValuePair<bool, object>(true, licensePermissionTemplateInfo);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, object>(false, "查询失败");
            }
        }

        public async Task<KeyValuePair<bool, object>> FirstDetails(Expression<Func<LicensePermissionTemplateInfo, bool>> where, CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<LicensePermissionTemplateInfo>();
                if (dbSet is null) return new KeyValuePair<bool, object>(false, "查询失败");
                dbSet.AsNoTracking();
                var licensePermissionTemplateInfo = await dbSet.Where(where)
                    .Include(b => b.LicenseApplicationInfo)
                    .ThenInclude(c => c.LicenseFeatureInfos)
                    .FirstOrDefaultAsync(cancellationToken: token);
                return new KeyValuePair<bool, object>(true, licensePermissionTemplateInfo);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, object>(false, "查询失败");
            }
        }
    }
}