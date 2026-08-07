using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Linq.Expressions;
using JayTom.Dws.Domain.Manager;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.LocalData;

namespace JayTom.Dws.Infrastructure.Repository.LocalData {

    public class PackageRepository : LocalRepositoryBase<PackageInfoModel>, IPackageRepository {

        /// <summary>
        /// 合并同一包裹时间戳的并发缓存加载，防止缓存未命中时重复查询数据库。
        /// </summary>
        private readonly ConcurrentDictionary<long, Lazy<Task<PackageInfoModel?>>> _packageLoads = new();

        public PackageRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<KeyValuePair<bool, List<PackageInfoModel>>> SelectPackageOrderByDescending<TOrder>(Expression<Func<PackageInfoModel, bool>> where, Expression<Func<PackageInfoModel, TOrder>> order, int pageIndex, int pageSize,
            CancellationToken token = default) {
            try {
                pageIndex = Math.Max(0, pageIndex);
                pageSize = Math.Clamp(pageSize, 1, 1000);
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<PackageInfoModel>();
                if (dbSet is null) return new KeyValuePair<bool, List<PackageInfoModel>>(false, new List<PackageInfoModel>());
                var barCodeInfoModels = await dbSet.AsNoTracking()
                    .AsSplitQuery()
                    .Include(b => b.BarCodeInfo)
                    .Include(b => b.WeightInfo)
                    .Include(b => b.VolumeInfo)
                    .Include(b => b.UploadInfo)
                    .Include(b => b.ExitInfo)
                    .Include(b => b.SortingInfo)
                    .ThenInclude(c => c.InstructionInfos)
                    .Include(b => b.ImageInfos)
                    .Include(b => b.LogisticsInfo)
                    .Include(b => b.OcrInfo)
                    .ThenInclude(c => c.OcrDetailedInfos)
                    .Include(b => b.CloudVideoUploadInfo)
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
                pageIndex = Math.Max(0, pageIndex);
                pageSize = Math.Clamp(pageSize, 1, 1000);
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<PackageInfoModel>();
                if (dbSet is null) return new KeyValuePair<bool, List<PackageInfoModel>>(false, new List<PackageInfoModel>());
                var barCodeInfoModels = await dbSet.AsNoTracking()
                    .AsSplitQuery()
                    .Include(b => b.BarCodeInfo)
                    .Include(b => b.WeightInfo)
                    .Include(b => b.VolumeInfo)
                    .Include(b => b.UploadInfo)
                    .Include(b => b.ExitInfo)
                    .Include(b => b.SortingInfo)
                    .ThenInclude(c => c.InstructionInfos)
                    .Include(b => b.ImageInfos)
                    .Include(b => b.LogisticsInfo)
                    .Include(b => b.OcrInfo)
                    .ThenInclude(c => c.OcrDetailedInfos)
                    .Include(b => b.CloudVideoUploadInfo)
                    .Where(where)
                    .OrderBy(order)
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
                    .AsSplitQuery()
                    .Where(where)
                    .Include(b => b.BarCodeInfo)
                    .Include(b => b.WeightInfo)
                    .Include(b => b.VolumeInfo)
                    .Include(b => b.UploadInfo)
                    .Include(b => b.ExitInfo)
                    .Include(b => b.SortingInfo)
                    .ThenInclude(c => c.InstructionInfos)
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

        public new async Task<int> Total(Expression<Func<PackageInfoModel, bool>> @where,
            CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<PackageInfoModel>();
                if (dbSet is null) return 0;
                return await dbSet.CountAsync(where, cancellationToken: token);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return 0;
            }
        }

        public async Task<PackageInfoModel?> GetMemoryCachePackageInfo(long packageTimestamped, CancellationToken token = default) {
            if (_cache.TryGetValue(packageTimestamped, out PackageInfoModel? cachedPackage)) {
                return cachedPackage;
            }

            var lazyLoad = _packageLoads.GetOrAdd(packageTimestamped,
                timestamp => new Lazy<Task<PackageInfoModel?>>(
                    () => LoadPackageAsync(timestamp, CancellationToken.None),
                    LazyThreadSafetyMode.ExecutionAndPublication));
            var loadTask = lazyLoad.Value;
            try {
                return await loadTask.WaitAsync(token);
            }
            finally {
                if (loadTask.IsCompleted)
                {
                    _packageLoads.TryRemove(
                        new KeyValuePair<long, Lazy<Task<PackageInfoModel?>>>(packageTimestamped, lazyLoad));
                }
                else
                {
                    _ = RemoveCompletedLoadAsync(packageTimestamped, lazyLoad, loadTask);
                }
            }
        }

        public void UpDateMemoryCachePackageInfo(PackageInfoModel info, CancellationToken token = default) {
            token.ThrowIfCancellationRequested();
            _cache.Set(info.PackageTimestamped, info, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(2)));
        }

        /// <summary>
        /// 插入一条数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public new async Task<bool> Insert(PackageInfoModel entity, CancellationToken token) {
            var insert = await base.Insert(entity, token);
            if (insert) {
                //加入缓存
                base._cache.Set(entity.PackageTimestamped, entity, new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(2)));
            }
            return insert;
        }

        /// <summary>
        /// 从数据库读取包裹并写入短期内存缓存。
        /// </summary>
        /// <param name="packageTimestamped">包裹时间戳。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>读取到的包裹；未找到或查询失败时返回空。</returns>
        private async Task<PackageInfoModel?> LoadPackageAsync(
            long packageTimestamped,
            CancellationToken token) {
            var result = await FirstOrDefaultInfo(
                package => package.PackageTimestamped == packageTimestamped, token);
            if (!result.Key || result.Value is null) {
                return null;
            }

            _cache.Set(packageTimestamped, result.Value, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(2)));
            return result.Value;
        }

        /// <summary>
        /// 在共享加载任务完成后清理对应的并发合并项。
        /// </summary>
        private async Task RemoveCompletedLoadAsync(
            long packageTimestamped,
            Lazy<Task<PackageInfoModel?>> lazyLoad,
            Task<PackageInfoModel?> loadTask)
        {
            try
            {
                await loadTask.ConfigureAwait(false);
            }
            catch
            {
                // 查询异常由仓储查询方法记录；这里只负责清理合并加载项。
            }
            finally
            {
                _packageLoads.TryRemove(
                    new KeyValuePair<long, Lazy<Task<PackageInfoModel?>>>(packageTimestamped, lazyLoad));
            }
        }
    }
}
