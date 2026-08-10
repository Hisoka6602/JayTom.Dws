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
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig.RuleConfig;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig {
    public class LogisticsSortingRepository : LocalRepositoryBase<LogisticsSortingInfoModel, SqliteConfContext>, ILogisticsSortingRepository {

        public LogisticsSortingRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<List<LogisticsSortingInfoModel>> LogisticsSortingItems(Expression<Func<LogisticsSortingInfoModel, bool>> where, CancellationToken token = default) {
            try {
                await using (var concardContext = _contextFactory.CreateDbContext()) {
                    var dbSet = concardContext?.Set<LogisticsSortingInfoModel>();
                    if (dbSet is null) return new List<LogisticsSortingInfoModel>();
                    return await dbSet.AsNoTracking()
                        .Where(where)
                        .OrderByDescending(o => o.CreateTime)
                        .Include(b => b.LogisticsRuleItems)
                       .ToListAsync(cancellationToken: token);
                    /*return await dbSet.AsNoTracking()
                        .Where(where)
                        .OrderByDescending(o => o.CreateTime)
                        .Include(b => b.LogisticsRuleItems)
                        .AsSingleQuery() // 添加AsSingleQuery
                        .Select(b => new LogisticsSortingInfoModel {
                            Remarks = b.Remarks,
                            CreateTime = b.CreateTime,
                            SortingName = b.SortingName,
                            ExitId = b.ExitId,
                            Id = b.Id,
                            ModifyTime = b.ModifyTime,
                            LogisticsRuleItems = b.LogisticsRuleItems
                                .Select(n => new LogisticsRuleInfoModel() {
                                    Id = n.Id,
                                    CreateTime = n.CreateTime,
                                    ModifyTime = n.ModifyTime,
                                    LogisticsSortingId = n.LogisticsSortingId,
                                    Remarks = n.Remarks,
                                    LogisticsId = n.LogisticsId,
                                }).ToList()
                        })
                        .ToListAsync(cancellationToken: token);*/
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error(e);
                return new List<LogisticsSortingInfoModel>();
            }
        }

        public async Task<bool> InsertDetailAsync(LogisticsSortingInfoModel entity, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var infoSet = concardContext?.Set<LogisticsSortingInfoModel>();

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

        public async Task<bool> InsertRangeDetailAsync(List<LogisticsSortingInfoModel> entities, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var infoSet = concardContext?.Set<LogisticsSortingInfoModel>();
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

        public async Task<bool> UpdateDetailAsync(LogisticsSortingInfoModel entity, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var infoSet = concardContext?.Set<LogisticsSortingInfoModel>();
                            var ruleSet = concardContext?.Set<LogisticsRuleInfoModel>();
                            // 先删除原有的
                            var ruleInfoModels = ruleSet?.AsTracking()?.Where(w => w.LogisticsSortingId.Equals(entity.Id))
                                ?.ToList() ?? new List<LogisticsRuleInfoModel>();
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
