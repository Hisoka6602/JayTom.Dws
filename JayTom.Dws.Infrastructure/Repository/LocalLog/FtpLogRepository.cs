using System;
using System.Linq;
using System.Text;
using EFCore.BulkExtensions;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.LocalLog;

namespace JayTom.Dws.Infrastructure.Repository.LocalLog {

    public class FtpLogRepository : LocalRepositoryBase<FtpLogInfoModel>, IFtpLogRepository {

        public FtpLogRepository(IDbContextFactory<SqliteLogsContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<KeyValuePair<bool, string>> DeleteDataThanDays(int days) {
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<FtpLogInfoModel>();
                if (dbSet is null) return new KeyValuePair<bool, string>(false, "数据实体为空");
                var batchDeleteAsync = await dbSet.AsNoTracking().Where(w => w.CreateTime <= DateTime.Now.AddDays(0 - days))
                    .BatchDeleteAsync();
                return new KeyValuePair<bool, string>(true, "删除成功");
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            return new KeyValuePair<bool, string>(false, "删除失败");
        }

        public async Task<KeyValuePair<bool, string>> DeleteEarliestData() {
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<FtpLogInfoModel>();
                if (dbSet is null) return new KeyValuePair<bool, string>(false, "数据实体为空");
                var model = await dbSet.AsNoTracking().OrderBy(o => o.CreateTime).FirstOrDefaultAsync();
                if (model is not null) {
                    var batchDeleteAsync = await dbSet.AsNoTracking().Where(w => w.CreateTime < model.CreateTime)
                        .BatchDeleteAsync();
                    return new KeyValuePair<bool, string>(true, "删除成功");
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            return new KeyValuePair<bool, string>(false, "删除失败");
        }
    }
}