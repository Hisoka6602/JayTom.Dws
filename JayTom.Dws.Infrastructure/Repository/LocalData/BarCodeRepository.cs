using System.Linq.Expressions;
using JayTom.Dws.Data.LocalConf;
using JayTom.Dws.Data.LocalData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;

namespace JayTom.Dws.Infrastructure.Repository.LocalData {

    public class BarCodeRepository : LocalRepositoryBase<BarCodeInfoModel>, IBarCodeRepository {

        public BarCodeRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<KeyValuePair<bool, List<BarCodeInfoModel>>> SelectBarCodeOrderByDescending<TOrder>(Expression<Func<BarCodeInfoModel, bool>> where,
            Expression<Func<BarCodeInfoModel, TOrder>> order,
            int pageIndex, int pageSize,
            CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<BarCodeInfoModel>();
                if (dbSet is null) return new KeyValuePair<bool, List<BarCodeInfoModel>>(false, new List<BarCodeInfoModel>());
                var barCodeInfoModels = await dbSet.AsNoTracking()
                    .Where(where)
                    .OrderByDescending(order)
                    .Include(b => b.ImageInfos)
                    .Include(b => b.VolumeInfo)
                    .Include(b => b.WeightInfo)
                    .Include(b => b.UploadInfo)
                    .Include(b => b.SortingInfo)
                    .Include(b => b.CloudVideoUploadInfo)
                    .Skip(pageIndex * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken: token);
                return new KeyValuePair<bool, List<BarCodeInfoModel>>(true, barCodeInfoModels);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, List<BarCodeInfoModel>>(false, new List<BarCodeInfoModel>());
            }
        }
    }
}