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

    public class OcrSortingRepository : LocalRepositoryBase<OcrSortingInfoModel>, IOcrSortingRepository {

        public OcrSortingRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
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
                        .ToListAsync(cancellationToken: token);
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error(e);
                return new List<OcrSortingInfoModel>();
            }
        }
    }
}