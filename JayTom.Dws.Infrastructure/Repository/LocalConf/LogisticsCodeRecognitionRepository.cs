using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf {

    public class LogisticsCodeRecognitionRepository : LocalRepositoryBase<LogisticsCodeRecognitionInfoModel>, ILogisticsCodeRecognitionRepository {

        public LogisticsCodeRecognitionRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<List<LogisticsCodeRecognitionInfoModel>> LogisticsCodes(Expression<Func<LogisticsCodeRecognitionInfoModel, bool>> where, CancellationToken token = default) {
            try {
                await using (var concardContext = _contextFactory.CreateDbContext()) {
                    var dbSet = concardContext?.Set<LogisticsCodeRecognitionInfoModel>();
                    if (dbSet is null) return new List<LogisticsCodeRecognitionInfoModel>();
                    return await dbSet.AsNoTracking()
                        .Where(where)
                        .OrderByDescending(o => o.CreateTime)
                        .Include(b => b.LogisticsRegexItems)
                        .AsSingleQuery() // 添加AsSingleQuery
                        .Select(b => new LogisticsCodeRecognitionInfoModel {
                            Remarks = b.Remarks,
                            CreateTime = b.CreateTime,
                            ExitId = b.ExitId,
                            Id = b.Id,
                            IconName = b.IconName,
                            IconBytes = b.IconBytes,
                            LogisticsCode = b.LogisticsCode,
                            LogisticsName = b.LogisticsName,
                            ModifyTime = b.ModifyTime,
                            SoundName = b.SoundName,
                            SoundBytes = b.SoundBytes,
                            LogisticsRegexItems = b.LogisticsRegexItems
                                .Select(n => new LogisticsRegexInfoModel {
                                    Id = n.Id,
                                    CreateTime = n.CreateTime,
                                    ModifyTime = n.ModifyTime,
                                    LogisticsId = n.LogisticsId,
                                    Remarks = n.Remarks,
                                    RegexPattern = n.RegexPattern
                                }).ToList()
                        })
                        .ToListAsync(cancellationToken: token);
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error(e);
                return new List<LogisticsCodeRecognitionInfoModel>();
            }
        }
    }
}