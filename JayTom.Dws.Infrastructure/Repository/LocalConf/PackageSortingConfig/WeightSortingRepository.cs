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

    public class WeightSortingRepository : LocalRepositoryBase<WeightSortingInfoModel, SqliteConfContext>, IWeightSortingRepository {

        public WeightSortingRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<List<WeightSortingInfoModel>> WeightSortingItems(Expression<Func<WeightSortingInfoModel, bool>> where, CancellationToken token = default) {
            try {
                await using (var concardContext = _contextFactory.CreateDbContext()) {
                    var dbSet = concardContext?.Set<WeightSortingInfoModel>();
                    if (dbSet is null) return new List<WeightSortingInfoModel>();
                    return await dbSet.AsNoTracking()
                        .Where(where)
                        .OrderByDescending(o => o.CreateTime)
                        .Include(b => b.WeightRuleItems)
                        .ToListAsync(cancellationToken: token);
                    /*return await dbSet.AsNoTracking()
                        .Where(where)
                        .OrderByDescending(o => o.CreateTime)
                        .Include(b => b.WeightRuleItems)
                        .AsSingleQuery() // 添加AsSingleQuery
                        .Select(b => new WeightSortingInfoModel {
                            Remarks = b.Remarks,
                            CreateTime = b.CreateTime,
                            SortingName = b.SortingName,
                            ExitId = b.ExitId,
                            Id = b.Id,
                            ModifyTime = b.ModifyTime,
                            WeightRuleItems = b.WeightRuleItems
                                .Select(n => new WeightRuleInfoModel {
                                    Id = n.Id,
                                    CreateTime = n.CreateTime,
                                    ModifyTime = n.ModifyTime,
                                    WeightSortingId = n.WeightSortingId,
                                    Remarks = n.Remarks,
                                    Formula = n.Formula,
                                }).ToList()
                        })
                        .ToListAsync(cancellationToken: token);*/
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error(e);
                return new List<WeightSortingInfoModel>();
            }
        }

        public async Task<bool> InsertDetailAsync(WeightSortingInfoModel entity, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var infoSet = concardContext?.Set<WeightSortingInfoModel>();

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

        public async Task<bool> InsertRangeDetailAsync(List<WeightSortingInfoModel> entities, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var infoSet = concardContext?.Set<WeightSortingInfoModel>();
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

        public async Task<bool> UpdateDetailAsync(WeightSortingInfoModel entity, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var infoSet = concardContext?.Set<WeightSortingInfoModel>();
                            var ruleSet = concardContext?.Set<WeightRuleInfoModel>();
                            // 先删除原有的
                            var ruleInfoModels = ruleSet?.AsTracking()?.Where(w => w.WeightSortingId.Equals(entity.Id))
                                ?.ToList() ?? new List<WeightRuleInfoModel>();
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
