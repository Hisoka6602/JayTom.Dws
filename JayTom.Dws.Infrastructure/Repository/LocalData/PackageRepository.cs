using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.LocalData;

namespace JayTom.Dws.Infrastructure.Repository.LocalData {

    public class PackageRepository : LocalRepositoryBase<PackageInfoModel>, IPackageRepository {

        public PackageRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<KeyValuePair<bool, List<PackageInfoModel>>> SelectPackageOrderByDescending<TOrder>(Expression<Func<PackageInfoModel, bool>> where, Expression<Func<PackageInfoModel, TOrder>> order, int pageIndex, int pageSize,
            CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<PackageInfoModel>();
                if (dbSet is null) return new KeyValuePair<bool, List<PackageInfoModel>>(false, new List<PackageInfoModel>());
                var barCodeInfoModels = await dbSet.AsNoTracking()
                    .Include(b => b.BarCodeInfo)
                    .Include(b => b.WeightInfo)
                    .Include(b => b.VolumeInfo)
                    .Include(b => b.UploadInfo)
                    .Include(b => b.ExitInfo)
                    .Include(b => b.SortingInfo)
                    .Include(b => b.LogisticsInfo)
                    .Include(b => b.OcrInfo)
                    .ThenInclude(c => c.OcrDetailedInfos)
                    .Include(b => b.ImageInfos)
                    .Include(b => b.CloudVideoUploadInfo)
                    .Include(b => b.AggregatePackagesInfo)
                    .Where(where)
                    .OrderByDescending(order)
                    .Skip(pageIndex * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken: token);
                return new KeyValuePair<bool, List<PackageInfoModel>>(true, barCodeInfoModels);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, List<PackageInfoModel>>(false, new List<PackageInfoModel>());
            }
        }

        public async Task<KeyValuePair<bool, List<PackageInfoModel>>> SelectPackage<TOrder>(Expression<Func<PackageInfoModel, bool>> where, Expression<Func<PackageInfoModel, TOrder>> order, int pageIndex, int pageSize,
            CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<PackageInfoModel>();
                if (dbSet is null) return new KeyValuePair<bool, List<PackageInfoModel>>(false, new List<PackageInfoModel>());
                var barCodeInfoModels = await dbSet.AsNoTracking()
                    .Include(b => b.BarCodeInfo)
                    .Include(b => b.WeightInfo)
                    .Include(b => b.VolumeInfo)
                    .Include(b => b.UploadInfo)
                    .Include(b => b.ExitInfo)
                    .Include(b => b.SortingInfo)
                    .Include(b => b.LogisticsInfo)
                    .Include(b => b.OcrInfo)
                    .ThenInclude(c => c.OcrDetailedInfos)
                    .Include(b => b.ImageInfos)
                    .Include(b => b.CloudVideoUploadInfo)
                    .Include(b => b.AggregatePackagesInfo)
                    .Where(where)
                    .Skip(pageIndex * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken: token);
                return new KeyValuePair<bool, List<PackageInfoModel>>(true, barCodeInfoModels);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, List<PackageInfoModel>>(false, new List<PackageInfoModel>());
            }
        }

        public async Task<KeyValuePair<bool, PackageInfoModel>> FirstOrDefaultInfo(Expression<Func<PackageInfoModel, bool>> where, CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<PackageInfoModel>();
                if (dbSet is null) return new KeyValuePair<bool, PackageInfoModel>(false, new PackageInfoModel());
                var barCodeInfoModels = await dbSet.AsNoTracking()
                    .Where(where)
                    .Include(b => b.BarCodeInfo)
                    .Include(b => b.WeightInfo)
                    .Include(b => b.VolumeInfo)
                    .Include(b => b.UploadInfo)
                    .Include(b => b.ExitInfo)
                    .Include(b => b.SortingInfo)
                    .Include(b => b.LogisticsInfo)
                    .Include(b => b.OcrInfo)
                    .ThenInclude(c => c.OcrDetailedInfos)
                    .Include(b => b.ImageInfos)
                    .Include(b => b.CloudVideoUploadInfo)
                    .Include(b => b.AggregatePackagesInfo)
                    .FirstOrDefaultAsync(cancellationToken: token);
                return new KeyValuePair<bool, PackageInfoModel>(true, barCodeInfoModels);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, PackageInfoModel>(false, new PackageInfoModel());
            }
        }

        public new async Task<int> Total([NotNull] Expression<Func<PackageInfoModel, bool>> @where,
            CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<PackageInfoModel>();
                if (dbSet is null) return 0;
                return await dbSet.AsNoTracking()
                     .Include(b => b.BarCodeInfo)
                     .Include(b => b.WeightInfo)
                     .Include(b => b.VolumeInfo)
                     .Include(b => b.UploadInfo)
                     .Include(b => b.ExitInfo)
                     .Include(b => b.SortingInfo)
                     .Include(b => b.LogisticsInfo)
                     .Include(b => b.OcrInfo)
                     .ThenInclude(c => c.OcrDetailedInfos)
                     .Include(b => b.ImageInfos)
                     .Include(b => b.CloudVideoUploadInfo)
                     .Include(b => b.AggregatePackagesInfo)
                     .Where(where)
                     .CountAsync(cancellationToken: token);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return 0;
            }
        }
    }
}