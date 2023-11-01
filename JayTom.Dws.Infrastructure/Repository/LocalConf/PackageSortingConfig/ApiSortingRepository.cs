using NLog;
using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq.Expressions;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore.Storage;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig {

    public class ApiSortingRepository : LocalRepositoryBase<ApiSortingInfoModel>, IApiSortingRepository {

        public ApiSortingRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<List<ApiSortingInfoModel>> ApiSortingItems(Expression<Func<ApiSortingInfoModel, bool>> where, CancellationToken token = default) {
            try {
                await using (var concardContext = _contextFactory.CreateDbContext()) {
                    var dbSet = concardContext?.Set<ApiSortingInfoModel>();
                    if (dbSet is null) return new List<ApiSortingInfoModel>();
                    return await dbSet.AsNoTracking()
                        .Where(where)
                        .OrderByDescending(o => o.CreateTime)
                        .Include(b => b.ApiRuleItems)
                        .ToListAsync(cancellationToken: token);
                    /*return await dbSet.AsNoTracking()
                        .Where(where)
                        .OrderByDescending(o => o.CreateTime)
                        .Include(b => b.ApiRuleItems)
                        .AsSingleQuery() // 添加AsSingleQuery
                        .Select(b => new ApiSortingInfoModel {
                            Remarks = b.Remarks,
                            CreateTime = b.CreateTime,
                            SortingName = b.SortingName,
                            ExitId = b.ExitId,
                            Id = b.Id,
                            ModifyTime = b.ModifyTime,
                            ApiRuleItems = b.ApiRuleItems
                                .Select(n => new ApiRuleInfoModel() {
                                    Id = n.Id,
                                    CreateTime = n.CreateTime,
                                    ModifyTime = n.ModifyTime,
                                    ApiSortingId = n.ApiSortingId,
                                    Remarks = n.Remarks,
                                    JsonContent = n.JsonContent,
                                }).ToList()
                        })
                        .ToListAsync(cancellationToken: token);*/
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error(e);
                return new List<ApiSortingInfoModel>();
            }
        }

        public async Task<bool> InsertDetailAsync(ApiSortingInfoModel entity, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var apiSortingInfoModels = concardContext?.Set<ApiSortingInfoModel>();
                            var apiRuleInfoModels = concardContext?.Set<ApiRuleInfoModel>();
                            var apiSortingInfoModel = new ApiSortingInfoModel() {
                                CreateTime = entity.CreateTime,
                                ModifyTime = entity.ModifyTime,
                                ExitId = entity.ExitId,
                                Remarks = entity.Remarks,
                                SortingName = entity.SortingName,
                            };
                            await apiSortingInfoModels.AddAsync(apiSortingInfoModel, token);
                            await concardContext?.SaveChangesAsync(token);
                            //先删除原有的
                            apiRuleInfoModels.RemoveRange(
                                apiRuleInfoModels.Where(w => w.ApiSortingId.Equals(apiSortingInfoModel.Id)));
                            if (entity.ApiRuleItems != null) {
                                foreach (var item in entity.ApiRuleItems) {
                                    var apiRuleInfoModel = new ApiRuleInfoModel() {
                                        CreateTime = item.CreateTime,
                                        ModifyTime = item.ModifyTime,
                                        Remarks = item.Remarks,
                                        JsonContent = item.JsonContent,
                                        ApiSortingInfo = apiSortingInfoModel // 将对象与引用的ApiSortingInfoModel关联
                                    };
                                    apiRuleInfoModels?.AddAsync(apiRuleInfoModel, token);
                                }
                                await concardContext?.SaveChangesAsync(token);
                            }
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

        public async Task<bool> InsertRangeDetailAsync(List<ApiSortingInfoModel> entities, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            foreach (var entity in entities) {
                                var apiSortingInfoModels = concardContext?.Set<ApiSortingInfoModel>();
                                var apiRuleInfoModels = concardContext?.Set<ApiRuleInfoModel>();
                                var apiSortingInfoModel = new ApiSortingInfoModel() {
                                    CreateTime = entity.CreateTime,
                                    ModifyTime = entity.ModifyTime,
                                    ExitId = entity.ExitId,
                                    Remarks = entity.Remarks,
                                    SortingName = entity.SortingName,
                                };
                                await apiSortingInfoModels.AddAsync(apiSortingInfoModel, token);
                                await concardContext?.SaveChangesAsync(token);
                                if (entity.ApiRuleItems != null) {
                                    foreach (var item in entity.ApiRuleItems) {
                                        var apiRuleInfoModel = new ApiRuleInfoModel() {
                                            CreateTime = item.CreateTime,
                                            ModifyTime = item.ModifyTime,
                                            Remarks = item.Remarks,
                                            JsonContent = item.JsonContent,
                                            ApiSortingInfo = apiSortingInfoModel // 将对象与引用的ApiSortingInfoModel关联
                                        };
                                        apiRuleInfoModels?.AddAsync(apiRuleInfoModel, token);
                                    }
                                    await concardContext?.SaveChangesAsync(token);
                                }
                            }
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

        public async Task<bool> UpdateDetailAsync(ApiSortingInfoModel entity, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var apiSortingInfoModels = concardContext?.Set<ApiSortingInfoModel>();
                            var apiRuleInfoModels = concardContext?.Set<ApiRuleInfoModel>();
                            // 先删除原有的
                            var ruleInfoModels = apiRuleInfoModels?.AsTracking()?.Where(w => w.ApiSortingId.Equals(entity.Id))
                                ?.ToList() ?? new List<ApiRuleInfoModel>();
                            concardContext.RemoveRange(ruleInfoModels);
                            apiSortingInfoModels.Update(entity);
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