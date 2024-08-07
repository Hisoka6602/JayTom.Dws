using Polly;
using System;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalData;

namespace JayTom.Dws.Infrastructure.Repository.LocalData {

    public class PackageRepository : LocalRepositoryBase<PackageInfoModel>, IPackageRepository {
        private static TimeSpan _cacheDuration = TimeSpan.FromSeconds(60);
        private SemaphoreSlim _cacheSlim = new(1);

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
                    .ThenInclude(c => c.InstructionInfos)
                    .Include(b => b.LogisticsInfo)
                    .Include(b => b.OcrInfo)
                    .ThenInclude(c => c.OcrDetailedInfos)
                    .Include(b => b.ImageInfos)
                    .Include(b => b.CloudVideoUploadInfo)
                    .Include(b => b.AggregatePackagesInfo)
                    .Include(b => b.NvrInfos)
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
                    .ThenInclude(c => c.InstructionInfos)
                    .Include(b => b.LogisticsInfo)
                    .Include(b => b.OcrInfo)
                    .ThenInclude(c => c.OcrDetailedInfos)
                    .Include(b => b.ImageInfos)
                    .Include(b => b.CloudVideoUploadInfo)
                    .Include(b => b.AggregatePackagesInfo)
                    .Include(b => b.NvrInfos)
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
                     .ThenInclude(c => c.InstructionInfos)
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

        public async Task<PackageInfoModel?> GetCachedPackage(long packageTimestamped, CancellationToken token = default) {
            try {
                await _cacheSlim.WaitAsync(token);
                if (!_cache.TryGetValue(packageTimestamped, out PackageInfoModel? package) ||
                    package is null) {
                    var (key, value) = await FirstOrDefaultInfo(w =>
                        w.PackageTimestamped.Equals(packageTimestamped), token);
                    if (key) {
                        package = value;
                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetSlidingExpiration(_cacheDuration);

                        // Save data in cache
                        _cache.Set(packageTimestamped, package, cacheEntryOptions);
                    }
                }

                return package;
            }
            finally {
                _cacheSlim.Release();
            }
        }

        private void UpdateCachedPackage(long packageTimestamped, PackageInfoModel packageInfo) {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(_cacheDuration);
            // 更新缓存
            _cache.Set(packageTimestamped, packageInfo, cacheEntryOptions);
        }

        public async Task<KeyValuePair<bool, List<PackageInfoModel>>> GetPackagesAround(long packageTimestamped, int amount, CancellationToken token = default) {
            try {
                // 创建数据库上下文
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<PackageInfoModel>();

                if (dbSet is null)
                    return new KeyValuePair<bool, List<PackageInfoModel>>(false, new List<PackageInfoModel>());
                var model = await dbSet.AsNoTracking().
                    FirstOrDefaultAsync(f =>
                        f.PackageTimestamped.Equals(packageTimestamped), cancellationToken: token);
                if (model is not null) {
                    // 获取时间戳之前的数据
                    var beforeTimestampData = await dbSet.AsNoTracking()
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
                        .Include(b => b.NvrInfos)
                        .Where(p => p.PackageCreateTime <= model.PackageCreateTime)
                        .OrderByDescending(p => p.PackageCreateTime)
                        .Take(amount + 1)
                        .ToListAsync(cancellationToken: token);

                    // 获取时间戳之后的数据
                    var afterTimestampData = await dbSet.AsNoTracking()
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
                        .Include(b => b.NvrInfos)
                        .Where(p => p.PackageCreateTime > model.PackageCreateTime)
                        .OrderBy(p => p.PackageCreateTime)
                        .Take(amount)
                        .ToListAsync(cancellationToken: token);
                    // 合并数据
                    var combinedData = beforeTimestampData.Concat(afterTimestampData).ToList();

                    return new KeyValuePair<bool, List<PackageInfoModel>>(true, combinedData);
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            return new KeyValuePair<bool, List<PackageInfoModel>>(false, new List<PackageInfoModel>());
        }

        public async Task<PackageInfoModel?> GetPackageDetails(long packageTimestamped, CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<PackageInfoModel>();
                if (dbSet is null) return null;
                var barCodeInfoModel = await dbSet.AsNoTracking()
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
                    .Include(b => b.NvrInfos)
                    .Where(w => w.PackageTimestamped.Equals(packageTimestamped))
                    .OrderByDescending(o => o.PackageCreateTime)
                    .FirstOrDefaultAsync(cancellationToken: token);
                return barCodeInfoModel;
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return null;
            }
        }

        public bool SetCacheDuration(TimeSpan duration) {
            _cacheDuration = duration; return true;
        }

        public async Task<bool> FillNavigationPropertyAsync<T>(long packageTimestamped, T property, int retryCount = 5) where T : class {
            // 定义重试策略
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(retryCount, attempt => TimeSpan.FromMilliseconds(300),
                    (exception, timeSpan, retryAttempt, context) => {
                        // 记录重试日志
                        NLog.LogManager.GetCurrentClassLogger().Warn($"Retry {retryAttempt} encountered an error: {exception.Message}. Waiting {timeSpan} before next retry.");
                    });

            // 使用重试策略执行方法
            return await retryPolicy.ExecuteAsync(async () => {
                await using var concardContext = _contextFactory.CreateDbContext();

                var packageInfo = await GetCachedPackage(packageTimestamped);
                if (packageInfo is not null) {
                    var packageInfoId = packageInfo.Id;

                    // 使用 EF Core 的元数据模型查找外键属性
                    var entityType = concardContext.Model.FindEntityType(typeof(T));
                    var foreignKey = entityType.GetForeignKeys().FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(PackageInfoModel));

                    if (foreignKey != null) {
                        var foreignKeyProperty = foreignKey.Properties.FirstOrDefault();
                        if (foreignKeyProperty != null) {
                            // 设置外键属性
                            var foreignKeyPropertyName = foreignKeyProperty.Name;
                            var propertyInfo = typeof(T).GetProperty(foreignKeyPropertyName);
                            if (propertyInfo != null) {
                                propertyInfo.SetValue(property, packageInfoId);
                            }

                            // 将数据添加到相应的表
                            concardContext.Set<T>().Add(property);
                        }
                    }
                    else {
                        // 动态处理嵌套导航属性
                        var navigationProperties = typeof(PackageInfoModel).GetProperties()
                            .Where(p => typeof(IEnumerable<object>).IsAssignableFrom(p.PropertyType) && p.PropertyType.IsGenericType)
                            .ToList();

                        bool added = false;
                        foreach (var navigationProperty in navigationProperties) {
                            var collectionType = navigationProperty.PropertyType.GetGenericArguments()[0];
                            if (collectionType == typeof(T)) {
                                var collection = (ICollection<T>)navigationProperty.GetValue(packageInfo);
                                if (collection != null) {
                                    // 设置外键属性
                                    var parentForeignKeyProperty = concardContext.Model.FindEntityType(collectionType)
                                        .GetForeignKeys()
                                        .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(PackageInfoModel))?
                                        .Properties.FirstOrDefault();
                                    if (parentForeignKeyProperty != null) {
                                        var parentForeignKeyPropertyName = parentForeignKeyProperty.Name;
                                        var parentForeignKeyPropertyInfo = typeof(T).GetProperty(parentForeignKeyPropertyName);
                                        if (parentForeignKeyPropertyInfo != null) {
                                            parentForeignKeyPropertyInfo.SetValue(property, packageInfoId);
                                        }
                                    }

                                    collection.Add(property);
                                    added = true;
                                    break;
                                }
                            }

                            var subProperties = collectionType.GetProperties()
                                .Where(p => typeof(IEnumerable<object>).IsAssignableFrom(p.PropertyType) && p.PropertyType.IsGenericType)
                                .ToList();

                            foreach (var subProperty in subProperties) {
                                var subCollectionType = subProperty.PropertyType.GetGenericArguments()[0];
                                if (subCollectionType == typeof(T)) {
                                    var parentEntity = navigationProperty.GetValue(packageInfo);
                                    if (parentEntity != null) {
                                        var subCollection = (ICollection<T>)subProperty.GetValue(parentEntity);
                                        if (subCollection != null) {
                                            // 设置外键属性
                                            var subForeignKeyProperty = concardContext.Model.FindEntityType(subCollectionType)
                                                .GetForeignKeys()
                                                .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == collectionType)?
                                                .Properties.FirstOrDefault();
                                            if (subForeignKeyProperty != null) {
                                                var subForeignKeyPropertyName = subForeignKeyProperty.Name;
                                                var subForeignKeyPropertyInfo = typeof(T).GetProperty(subForeignKeyPropertyName);
                                                if (subForeignKeyPropertyInfo != null) {
                                                    var parentEntityIdProperty = collectionType.GetProperty("Id");
                                                    if (parentEntityIdProperty != null) {
                                                        var parentEntityId = parentEntityIdProperty.GetValue(parentEntity);
                                                        subForeignKeyPropertyInfo.SetValue(property, parentEntityId);
                                                    }
                                                }
                                            }

                                            subCollection.Add(property);
                                            added = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (added) break;
                        }
                    }

                    var saveChangesAsync = await concardContext.SaveChangesAsync();
                    if (saveChangesAsync > 0) {
                        // 更新缓存
                        UpdateCachedPackage(packageTimestamped, packageInfo);
                        return true;
                    }
                }

                return false;
            });
        }
    }
}