using NLog;
using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore.Storage;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig {

    public class OcrSortingRepository : LocalRepositoryBase<OcrSortingInfoModel, SqliteConfContext>, IOcrSortingRepository {

        public OcrSortingRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<List<OcrSortingInfoModel>> OcrSortingItems(Expression<Func<OcrSortingInfoModel, bool>> where, CancellationToken token = default) {
            try {
                await using (var concardContext = _contextFactory.CreateDbContext()) {
                    var dbSet = concardContext?.Set<OcrSortingInfoModel>();
                    if (dbSet is null) return new List<OcrSortingInfoModel>();
                    return await dbSet.AsNoTracking()
                        .Where(where)
                        .OrderByDescending(o => o.CreateTime)
                        .Include(b => b.OcrRuleItems)
                        .ToListAsync(cancellationToken: token);
                    /*return await dbSet.AsNoTracking()
                        .Where(where)
                        .OrderByDescending(o => o.CreateTime)
                        .Include(b => b.OcrRuleItems)
                        .AsSingleQuery() // 添加AsSingleQuery
                        .Select(b => new OcrSortingInfoModel {
                            Remarks = b.Remarks,
                            CreateTime = b.CreateTime,
                            SortingName = b.SortingName,
                            ExitId = b.ExitId,
                            Id = b.Id,
                            ModifyTime = b.ModifyTime,
                            OcrRuleItems = b.OcrRuleItems
                                .Select(n => new OcrRuleInfoModel() {
                                    Id = n.Id,
                                    CreateTime = n.CreateTime,
                                    ModifyTime = n.ModifyTime,
                                    OcrSortingId = n.OcrSortingId,
                                    Remarks = n.Remarks,
                                    RegexPattern = n.RegexPattern,
                                }).ToList()
                        })
                        .ToListAsync(cancellationToken: token);*/
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error(e);
                return new List<OcrSortingInfoModel>();
            }
        }

        public async Task<bool> InsertDetailAsync(OcrSortingInfoModel entity, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var infoSet = concardContext?.Set<OcrSortingInfoModel>();

                            EntityGraphWriteGuard.ClearDependentReferenceNavigations(concardContext, entity);
                            await infoSet.AddAsync(entity, token);
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

        public async Task<bool> InsertRangeDetailAsync(List<OcrSortingInfoModel> entities, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var infoSet = concardContext?.Set<OcrSortingInfoModel>();
                            EntityGraphWriteGuard.ClearDependentReferenceNavigations(concardContext, entities);
                            await infoSet.AddRangeAsync(entities, token);
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

        public async Task<bool> UpdateDetailAsync(OcrSortingInfoModel entity, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var infoSet = concardContext?.Set<OcrSortingInfoModel>();
                            var ruleSet = concardContext?.Set<OcrRuleInfoModel>();
                            // 先删除原有的
                            var ruleInfoModels = ruleSet?.AsTracking()?.Where(w => w.OcrSortingId.Equals(entity.Id))
                                ?.ToList() ?? new List<OcrRuleInfoModel>();
                            concardContext.RemoveRange(ruleInfoModels);
                            EntityGraphWriteGuard.ClearDependentReferenceNavigations(concardContext, entity);
                            infoSet.Update(entity);
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
    }
}
