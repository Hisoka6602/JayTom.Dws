using System;
using System.Linq;
using System.Text;
using RTools_NTS.Util;
using System.Threading.Tasks;
using JayTom.Dws.Data.License;
using System.Linq.Expressions;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.License;

namespace JayTom.Dws.Infrastructure.Repository.License {

    public class LicenseUserRepository :
        MemoryCacheRepositoryBase<LicenseUserInfo, LicenseApiContext>, ILicenseUserRepository {

        public LicenseUserRepository(IDbContextFactory<LicenseApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<KeyValuePair<bool, object>> DetailsInfo(string userCode, CancellationToken token) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<LicenseUserInfo>();
                if (dbSet is null) return new KeyValuePair<bool, object>(false, "查询失败");
                IQueryable<LicenseUserInfo> query = dbSet.AsNoTracking();
                var licenseUserInfo = await query.Where(w => w.UserCode.Equals(userCode))
                    .Include(b => b.UserDetailsInfo)
                    .Include(b => b.AppLicenseInfos)
                    .Include(b => b.LicenseCodeInfos)
                    .FirstOrDefaultAsync(cancellationToken: token);
                return new KeyValuePair<bool, object>(true, licenseUserInfo);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, object>(false, "查询失败");
            }
        }

        public new async Task<KeyValuePair<bool, object>> SelectOrderByDescending<TOrder>(Expression<Func<LicenseUserInfo, bool>> where, Expression<Func<LicenseUserInfo, TOrder>> order, CancellationToken token) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<LicenseUserInfo>();
                if (dbSet is null) return new KeyValuePair<bool, object>(false, "查询失败");
                IQueryable<LicenseUserInfo> query = dbSet.AsNoTracking();
                var licenseUserInfos = await query.Where(where)
                    .Include(b => b.UserDetailsInfo)
                    .Include(b => b.AppLicenseInfos)
                    .Include(b => b.LicenseCodeInfos)
                    .ToListAsync(cancellationToken: token);
                return new KeyValuePair<bool, object>(true, licenseUserInfos);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, object>(false, "查询失败");
            }
        }

        public async Task<KeyValuePair<bool, object>> UpdateTenantLicenseMaxCount(string userCode, long licensePermissionTemplateInfoId, int maxLicenseCodeCount,
            CancellationToken cancellationToken) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<LicenseAppLicenseInfo>();
                var licenseUserInfos = concardContext?.Set<LicenseUserInfo>();
                if (dbSet is null || licenseUserInfos is null) return new KeyValuePair<bool, object>(false, "操作失败");
                var licenseUserInfo = await ((IQueryable<LicenseUserInfo>)licenseUserInfos)
                    .FirstOrDefaultAsync(f => f.UserCode.Equals(userCode), cancellationToken: cancellationToken);
                if (licenseUserInfo is not null) {
                    IQueryable<LicenseAppLicenseInfo> query = dbSet;
                    var info = await query.Where(w => w.UserId.Equals(licenseUserInfo.Id) &&
                                                      w.LicensePermissionTemplateInfoId.Equals(licensePermissionTemplateInfoId))
                        .FirstOrDefaultAsync(cancellationToken: cancellationToken);

                    if (info is null) {
                        dbSet.Add(new LicenseAppLicenseInfo() {
                            CreateTime = DateTime.Now,
                            UserId = licenseUserInfo.Id,
                            LicensePermissionTemplateInfoId = licensePermissionTemplateInfoId,
                            MaxLicenseCodeCount = maxLicenseCodeCount,
                            ModifyTime = DateTime.Now
                        });
                    }
                    else {
                        info.MaxLicenseCodeCount = maxLicenseCodeCount;
                        dbSet.Update(info);
                    }

                    var changesAsync = await concardContext?.SaveChangesAsync(cancellationToken);
                    return changesAsync > 0 ? new KeyValuePair<bool, object>(true, "修改成功") : new KeyValuePair<bool, object>(false, "修改失败");
                }
                else {
                    return new KeyValuePair<bool, object>(false, "用户不存在");
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, object>(false, "修改失败");
            }
        }
    }
}
