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
using JayTom.Dws.Models.LocalConf.CameraConfig;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig {

    public class SortingInstructionBindingRepository : LocalRepositoryBase<SortingInstructionBindingInfoModel, SqliteConfContext>, ISortingInstructionBindingRepository {

        public SortingInstructionBindingRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<List<SortingInstructionBindingInfoModel>> InstructionBindings(Expression<Func<SortingInstructionBindingInfoModel, bool>> where, CancellationToken token = default) {
            try {
                await using (var concardContext = _contextFactory.CreateDbContext()) {
                    var dbSet = concardContext?.Set<SortingInstructionBindingInfoModel>();
                    if (dbSet is null) return new List<SortingInstructionBindingInfoModel>();
                    return await dbSet.AsNoTracking()
                        .Where(where)
                        .OrderByDescending(o => o.CreateTime)
                        .Include(b => b.InstructionItems)
                        .ToListAsync(cancellationToken: token);
                    /*return await dbSet.AsNoTracking()
                        .Where(where)
                        .OrderByDescending(o => o.CreateTime)
                        .Include(b => b.InstructionItems)
                        .AsSingleQuery() // 添加AsSingleQuery
                        .Select(b => new SortingInstructionBindingInfoModel {
                            Remarks = b.Remarks,
                            CreateTime = b.CreateTime,
                            ExitId = b.ExitId,
                            Id = b.Id,
                            DelaySendMilliseconds = b.DelaySendMilliseconds,
                            SendIntervalMilliseconds = b.SendIntervalMilliseconds,
                            IsActive = b.IsActive,
                            ModifyTime = b.ModifyTime,
                            InstructionItems = b.InstructionItems
                                .Select(n => new SortingInstructionInfoModel {
                                    Id = n.Id,
                                    CreateTime = n.CreateTime,
                                    ModifyTime = n.ModifyTime,
                                    InstructionBindingId = n.InstructionBindingId,
                                    Remarks = n.Remarks,
                                    Instruction = n.Instruction,
                                    ReplyContent = n.ReplyContent,
                                }).ToList()
                        })
                        .ToListAsync(cancellationToken: token);*/
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error(e);
                return new List<SortingInstructionBindingInfoModel>();
            }
        }

        public async Task<bool> InsertDetailAsync(SortingInstructionBindingInfoModel entity, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var infoSet = concardContext?.Set<SortingInstructionBindingInfoModel>();

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

        public async Task<bool> InsertRangeDetailAsync(List<SortingInstructionBindingInfoModel> entities, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var infoSet = concardContext?.Set<SortingInstructionBindingInfoModel>();
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

        public async Task<bool> UpdateDetailAsync(SortingInstructionBindingInfoModel entity, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var infoSet = concardContext?.Set<SortingInstructionBindingInfoModel>();
                            var ruleSet = concardContext?.Set<SortingInstructionInfoModel>();
                            // 先删除原有的
                            var ruleInfoModels = ruleSet?.AsTracking()?.Where(w => w.InstructionBindingId.Equals(entity.Id))
                                ?.ToList() ?? new List<SortingInstructionInfoModel>();
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
