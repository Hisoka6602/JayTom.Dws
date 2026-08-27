using NLog;
using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using JayTom.Dws.Models.License;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Legacy.Contracts.Repositories.License;
using Microsoft.EntityFrameworkCore.Storage;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig.RuleConfig;

namespace JayTom.Dws.Infrastructure.Repository.License {

    public class LicenseCodeRepository :
        MemoryCacheRepositoryBase<LicenseCodeInfo, LicenseApiContext>, ILicenseCodeRepository {

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
                    .Include(e => e.LicenseGroupInfo)
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
                    .Include(e => e.LicenseGroupInfo)
                    .FirstOrDefaultAsync(cancellationToken: token);
                return new KeyValuePair<bool, object>(true, applicationInfo);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, object>(false, "查询失败");
            }
        }

        public new async Task<bool> UpdateRange(List<LicenseCodeInfo> entities, CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var licenseCodeInfos = concardContext?.Set<LicenseCodeInfo>();
                            licenseCodeInfos.UpdateRange(entities);
                            await concardContext?.SaveChangesAsync(token);
                            await contextTransaction.CommitAsync(token);
                            return true;
                        }
                    }

                    return false;
                });
            }
            catch (Win32Exception) {
                await contextTransaction?.RollbackAsync(token)!;
            }
            catch (TaskCanceledException) {
                await contextTransaction?.RollbackAsync(token)!;
            }
            catch (Exception e) {
                await contextTransaction?.RollbackAsync(token)!;
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return false;
        }

        public new async Task<bool> InsertRange(List<LicenseCodeInfo> entities, CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var licenseCodeInfos = concardContext?.Set<LicenseCodeInfo>();
                            licenseCodeInfos.AddRange(entities);
                            await concardContext?.SaveChangesAsync(token);
                            await contextTransaction.CommitAsync(token);
                            return true;
                        }
                    }

                    return false;
                });
            }
            catch (Win32Exception) {
                await contextTransaction?.RollbackAsync(token)!;
            }
            catch (TaskCanceledException) {
                await contextTransaction?.RollbackAsync(token)!;
            }
            catch (Exception e) {
                await contextTransaction?.RollbackAsync(token)!;
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return false;
        }

        public async Task<bool> UnbindMachineCode(string machineCode, CancellationToken token) {
            //事务
            //删除机器码
            //修改激活数量

            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var licenseCodeInfos = concardContext?.Set<LicenseCodeInfo>();

                            var licenseClientBindingInfos = concardContext?.Set<LicenseClientBindingInfo>();
                            if (licenseCodeInfos is not null && licenseClientBindingInfos is not null) {
                                var licenseClientBindingInfo = licenseClientBindingInfos.FirstOrDefault(f => f.MachineCode.Equals(machineCode));

                                if (licenseClientBindingInfo is not null) {
                                    licenseClientBindingInfos.Remove(licenseClientBindingInfo);
                                    var licenseCodeInfo = licenseCodeInfos.FirstOrDefault(f =>
                                        f.Id.Equals(licenseClientBindingInfo.LicenseCodeId));
                                    if (licenseCodeInfo is not null) {
                                        licenseCodeInfo.ActivatedClientCount -= 1;
                                        licenseCodeInfo.ActivatedClientCount = licenseCodeInfo.ActivatedClientCount < 0
                                            ? 0
                                            : licenseCodeInfo.ActivatedClientCount;
                                        licenseCodeInfos.Update(licenseCodeInfo);
                                        await concardContext?.SaveChangesAsync(token);
                                        await contextTransaction.CommitAsync(token);
                                        return true;
                                    }
                                }
                            }
                        }
                    }

                    return false;
                });
            }
            catch (Win32Exception) {
                await contextTransaction?.RollbackAsync(token)!;
            }
            catch (TaskCanceledException) {
                await contextTransaction?.RollbackAsync(token)!;
            }
            catch (Exception e) {
                await contextTransaction?.RollbackAsync(token)!;
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return false;
        }
    }
}
