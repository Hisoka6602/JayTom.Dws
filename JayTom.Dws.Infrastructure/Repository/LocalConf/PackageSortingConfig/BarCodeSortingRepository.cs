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
    public class BarCodeSortingRepository : LocalRepositoryBase<BarCodeSortingInfoModel>, IBarCodeSortingRepository {

        public BarCodeSortingRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<List<BarCodeSortingInfoModel>> BarCodeSortingItems(Expression<Func<BarCodeSortingInfoModel, bool>> where, CancellationToken token = default) {
            try {
                await using (var concardContext = _contextFactory.CreateDbContext()) {
                    var dbSet = concardContext?.Set<BarCodeSortingInfoModel>();
                    if (dbSet is null) return new List<BarCodeSortingInfoModel>();
                    return await dbSet.AsNoTracking()
                        .Where(where)
                        .OrderByDescending(o => o.CreateTime)
                        .Include(b => b.BarCodeRegexItems)
                        .AsSingleQuery() // 添加AsSingleQuery
                        .Select(b => new BarCodeSortingInfoModel {
                            Remarks = b.Remarks,
                            CreateTime = b.CreateTime,
                            ExitId = b.ExitId,
                            SortingName = b.SortingName,
                            Id = b.Id,
                            ModifyTime = b.ModifyTime,
                            BarCodeRegexItems = b.BarCodeRegexItems
                                .Select(n => new BarCodeRegexInfoModel {
                                    Id = n.Id,
                                    CreateTime = n.CreateTime,
                                    ModifyTime = n.ModifyTime,
                                    BarCodeSortingId = n.BarCodeSortingId,
                                    Remarks = n.Remarks,
                                    RegexPattern = n.RegexPattern
                                }).ToList()
                        })
                        .ToListAsync(cancellationToken: token);
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error(e);
                return new List<BarCodeSortingInfoModel>();
            }
        }
    }
}