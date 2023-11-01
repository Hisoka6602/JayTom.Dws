using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig {

    public class WeightSortingRepository : LocalRepositoryBase<WeightSortingInfoModel>, IWeightSortingRepository {

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
    }
}