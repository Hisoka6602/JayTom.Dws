using NLog;
using System.Linq.Expressions;
using JayTom.Dws.Domain.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JayTom.Dws.Infrastructure.Repository {
    public class MemoryCacheRepositoryBase<T> : RepositoryBase<T>, IMemoryCacheRepository<T> where T : class {
        private readonly IMemoryCache _cache;

        /// <summary>
        /// 合并同一实体类型的并发首次加载，防止缓存击穿。
        /// </summary>
        private static readonly SemaphoreSlim LoadLock = new(1, 1);

        /// <summary>
        /// 保存稳定且不会与其他实体类型冲突的缓存键。
        /// </summary>
        private static readonly string CacheKey = typeof(T).FullName ?? typeof(T).Name;

        /// <summary>
        /// 缓存 EF 模型中的导航属性名称，避免每次缓存刷新后重复解析模型。
        /// </summary>
        private static string[]? _navigationPropertyNames;

        /// <summary>
        /// 标记缓存失效代次，避免并发加载在写入完成后回填旧快照。
        /// </summary>
        private static long _cacheGeneration;

        public MemoryCacheRepositoryBase(IDbContextFactory<DbContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
            _cache = cache;
        }

        public async Task<List<T>> MemoryCacheData() {
            if (_cache.TryGetValue(CacheKey, out List<T>? cachedItems) &&
                cachedItems is not null) {
                return cachedItems;
            }

            using var loadLease =
                await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(
                    LoadLock, CancellationToken.None);
            if (_cache.TryGetValue(CacheKey, out cachedItems) &&
                cachedItems is not null) {
                return cachedItems;
            }

            try {
                await using var context = _contextFactory.CreateDbContext();
                IQueryable<T> query = context.Set<T>();
                foreach (var navigationPropertyName in GetNavigationPropertyNames(context)) {
                    query = query.Include(navigationPropertyName);
                }

                List<T> items;
                long observedGeneration;
                do {
                    observedGeneration = Volatile.Read(ref _cacheGeneration);
                    items = await query.AsNoTracking().ToListAsync();
                } while (observedGeneration != Volatile.Read(ref _cacheGeneration));

                _cache.Set(CacheKey, items, TimeSpan.FromMinutes(5));
                return items;
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return new List<T>();
        }

        public void UpdateMemoryCache() {
            try {
                InvalidateCache();
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
        }

        public new async Task<bool> Insert(T entity, CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(LoadLock, token);
            var insert = await base.Insert(entity, token);
            if (insert) {
                InvalidateCache();
            }
            return insert;
        }

        public new async Task InsertAsync(T entity, CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(LoadLock, token);
            await base.InsertAsync(entity, token);
            InvalidateCache();
        }

        public new async Task<bool> Update(T entity, CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(LoadLock, token);
            var update = await base.Update(entity, token);
            if (update) {
                InvalidateCache();
            }
            return update;
        }

        public new async Task<bool> Delete(T entity, CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(LoadLock, token);
            var delete = await base.Delete(entity, token);
            if (delete) {
                InvalidateCache();
            }
            return delete;
        }

        public new async Task<int> DeleteCount(int count, CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(LoadLock, token);
            var deleteCount = await base.DeleteCount(count, token);
            if (deleteCount > 0) {
                InvalidateCache();
            }

            return deleteCount;
        }

        public new async Task<int> DeleteCount(int count, Expression<Func<T, bool>> @where, CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(LoadLock, token);
            var deleteCount = await base.DeleteCount(count, @where, token);
            if (deleteCount > 0) {
                InvalidateCache();
            }

            return deleteCount;
        }

        public new async Task<bool> DeleteRange(List<T> entities, CancellationToken token = default) {
            using var cacheLease =
                await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(LoadLock, token);
            var deleteRange = await base.DeleteRange(entities, token);
            if (deleteRange) {
                InvalidateCache();
            }

            return deleteRange;
        }
        public new async Task<bool> InsertOrUpdate(T entity, CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(LoadLock, token);
            var insertOrUpdate = await base.InsertOrUpdate(entity, token);
            if (insertOrUpdate) {
                InvalidateCache();
            }

            return insertOrUpdate;
        }

        public new async Task<bool> InsertOrUpdateRange(List<T> entities,
            CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(LoadLock, token);
            var insertOrUpdateRange = await base.InsertOrUpdateRange(entities, token);
            if (insertOrUpdateRange) {
                InvalidateCache();
            }

            return insertOrUpdateRange;
        }

        public new async Task<bool> SyncEntities(List<T> entities, CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(LoadLock, token);
            var syncEntities = await base.SyncEntities(entities, token);
            if (syncEntities) {
                InvalidateCache();
            }
            return syncEntities;
        }

        /// <summary>
        /// 推进缓存代次并移除当前实体快照。
        /// </summary>
        private void InvalidateCache() {
            Interlocked.Increment(ref _cacheGeneration);
            _cache.Remove(CacheKey);
        }

        /// <summary>
        /// 获取并缓存当前实体的导航属性名称。
        /// </summary>
        /// <param name="context">包含实体模型元数据的数据库上下文。</param>
        /// <returns>导航属性名称快照。</returns>
        private static string[] GetNavigationPropertyNames(DbContext context) {
            var cachedNames = Volatile.Read(ref _navigationPropertyNames);
            if (cachedNames is not null) {
                return cachedNames;
            }

            var discoveredNames = context.Model.FindEntityType(typeof(T))?
                .GetNavigations()
                .Select(navigation => navigation.Name)
                .ToArray() ?? [];
            Interlocked.CompareExchange(
                ref _navigationPropertyNames, discoveredNames, null);
            return Volatile.Read(ref _navigationPropertyNames) ?? discoveredNames;
        }
    }
}
