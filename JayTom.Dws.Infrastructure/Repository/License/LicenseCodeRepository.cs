using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.License;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.License;

namespace JayTom.Dws.Infrastructure.Repository.License {

    public class LicenseCodeRepository :
        MemoryCacheRepositoryBase<LicenseCodeInfo>, ILicenseCodeRepository {

        public LicenseCodeRepository(IDbContextFactory<LicenseApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<KeyValuePair<bool, object>> Details(Expression<Func<LicenseCodeInfo, bool>> where, CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<LicenseCodeInfo>();
                if (dbSet is null) return new KeyValuePair<bool, object>(false, "查询失败");
                dbSet.AsNoTracking();
                var applicationInfo = await dbSet.Where(where)
                    .Include(b => b.LicensePermissionTemplateInfo)
                    .Include(c => c.UserInfo)
                    .Include(d => d.LicenseClientBindingInfo)
                    .ToListAsync(cancellationToken: token);
                return new KeyValuePair<bool, object>(true, applicationInfo);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, object>(false, "查询失败");
            }
        }

        public async Task<KeyValuePair<bool, object>> FirstDetails(Expression<Func<LicenseCodeInfo, bool>> where, CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<LicenseCodeInfo>();
                if (dbSet is null) return new KeyValuePair<bool, object>(false, "查询失败");
                dbSet.AsNoTracking();
                var applicationInfo = await dbSet.Where(where)
                    .Include(b => b.LicensePermissionTemplateInfo)
                    .Include(c => c.UserInfo)
                    .Include(d => d.LicenseClientBindingInfo)
                    .FirstOrDefaultAsync(cancellationToken: token);
                return new KeyValuePair<bool, object>(true, applicationInfo);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, object>(false, "查询失败");
            }
        }
    }
}