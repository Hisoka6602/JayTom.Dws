using NLog;
using System.Linq.Expressions;
using JayTom.Dws.Legacy.Contracts.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JayTom.Dws.Infrastructure.Repository {
    public class MemoryCacheRepositoryBase<T, TContext> : RepositoryBase<T, TContext>, IMemoryCacheRepository<T>
        where T : class
        where TContext : DbContext {
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

        public MemoryCacheRepositoryBase(IDbContextFactory<TContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<List<T>> MemoryCacheData(CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            if (_cache.TryGetValue(CacheKey, out List<T>? cachedItems) &&
                cachedItems is not null) {
                return cachedItems;
            }

            using var loadLease =
                await global::JayTom.Dws.Abstractions.Threading.SemaphoreLease.EnterAsync(
                    LoadLock, cancellationToken);
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
                    items = await query.AsNoTracking().ToListAsync(cancellationToken);
                } while (observedGeneration != Volatile.Read(ref _cacheGeneration));

                _cache.Set(CacheKey, items, TimeSpan.FromMinutes(5));
                return items;
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return new List<T>();
        }

        public Task UpdateMemoryCache(CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            try {
                InvalidateCache();
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return Task.CompletedTask;
        }

        public override async Task<bool> Insert(T entity, CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Abstractions.Threading.SemaphoreLease.EnterAsync(LoadLock, token);
            var insert = await base.Insert(entity, token);
            if (insert) {
                InvalidateCache();
            }
            return insert;
        }

        public override Task<bool> InsertAsync(T entity, CancellationToken token) =>
            Insert(entity, token);

        /// <summary>批量插入成功后使实体缓存失效。</summary>
        public override async Task<bool> InsertRange(List<T> entities, CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Abstractions.Threading.SemaphoreLease.EnterAsync(LoadLock, token);
            var succeeded = await base.InsertRange(entities, token);
            if (succeeded) {
                InvalidateCache();
            }

            return succeeded;
        }

        public override async Task<bool> Update(T entity, CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Abstractions.Threading.SemaphoreLease.EnterAsync(LoadLock, token);
            var update = await base.Update(entity, token);
            if (update) {
                InvalidateCache();
            }
            return update;
        }

        public override async Task<bool> Delete(T entity, CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Abstractions.Threading.SemaphoreLease.EnterAsync(LoadLock, token);
            var delete = await base.Delete(entity, token);
            if (delete) {
                InvalidateCache();
            }
            return delete;
        }

        public override async Task<int> DeleteCount(int count, CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Abstractions.Threading.SemaphoreLease.EnterAsync(LoadLock, token);
            var deleteCount = await base.DeleteCount(count, token);
            if (deleteCount > 0) {
                InvalidateCache();
            }

            return deleteCount;
        }

        public override async Task<int> DeleteCount(int count, Expression<Func<T, bool>> @where, CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Abstractions.Threading.SemaphoreLease.EnterAsync(LoadLock, token);
            var deleteCount = await base.DeleteCount(count, @where, token);
            if (deleteCount > 0) {
                InvalidateCache();
            }

            return deleteCount;
        }

        public override async Task<bool> DeleteRange(List<T> entities, CancellationToken token = default) {
            using var cacheLease =
                await global::JayTom.Dws.Abstractions.Threading.SemaphoreLease.EnterAsync(LoadLock, token);
            var deleteRange = await base.DeleteRange(entities, token);
            if (deleteRange) {
                InvalidateCache();
            }

            return deleteRange;
        }
        public override async Task<bool> InsertOrUpdate(T entity, CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Abstractions.Threading.SemaphoreLease.EnterAsync(LoadLock, token);
            var insertOrUpdate = await base.InsertOrUpdate(entity, token);
            if (insertOrUpdate) {
                InvalidateCache();
            }

            return insertOrUpdate;
        }

        public override async Task<bool> InsertOrUpdateRange(List<T> entities,
            CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Abstractions.Threading.SemaphoreLease.EnterAsync(LoadLock, token);
            var insertOrUpdateRange = await base.InsertOrUpdateRange(entities, token);
            if (insertOrUpdateRange) {
                InvalidateCache();
            }

            return insertOrUpdateRange;
        }

        /// <summary>按排除字段写入成功后使实体缓存失效。</summary>
        public override async Task<bool> InsertOrUpdate(
            T entity,
            Expression<Func<T, object>> excludeColumns,
            CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Abstractions.Threading.SemaphoreLease.EnterAsync(LoadLock, token);
            var succeeded = await base.InsertOrUpdate(entity, excludeColumns, token);
            if (succeeded) {
                InvalidateCache();
            }

            return succeeded;
        }

        /// <summary>按排除字段批量写入成功后使实体缓存失效。</summary>
        public override async Task<bool> InsertOrUpdateRange(
            List<T> entities,
            Expression<Func<T, object>> excludeColumns,
            CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Abstractions.Threading.SemaphoreLease.EnterAsync(LoadLock, token);
            var succeeded = await base.InsertOrUpdateRange(entities, excludeColumns, token);
            if (succeeded) {
                InvalidateCache();
            }

            return succeeded;
        }

        /// <summary>批量更新成功后使实体缓存失效。</summary>
        public override async Task<bool> UpdateRange(List<T> entities, CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Abstractions.Threading.SemaphoreLease.EnterAsync(LoadLock, token);
            var succeeded = await base.UpdateRange(entities, token);
            if (succeeded) {
                InvalidateCache();
            }

            return succeeded;
        }

        /// <summary>按排除字段批量更新成功后使实体缓存失效。</summary>
        public override async Task<bool> UpdateRange(
            List<T> entities,
            Expression<Func<T, object>> excludeColumns,
            CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Abstractions.Threading.SemaphoreLease.EnterAsync(LoadLock, token);
            var succeeded = await base.UpdateRange(entities, excludeColumns, token);
            if (succeeded) {
                InvalidateCache();
            }

            return succeeded;
        }

        public override async Task<bool> SyncEntities(List<T> entities, CancellationToken token) {
            using var cacheLease =
                await global::JayTom.Dws.Abstractions.Threading.SemaphoreLease.EnterAsync(LoadLock, token);
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
