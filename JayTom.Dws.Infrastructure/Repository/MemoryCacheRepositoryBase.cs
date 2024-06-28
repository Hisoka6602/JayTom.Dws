using NLog;
using System.Reflection;
using System.Linq.Expressions;
using JayTom.Dws.Domain.Repository;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Infrastructure.Repository {

    public class MemoryCacheRepositoryBase<T> : RepositoryBase<T>, IMemoryCacheRepository<T> where T : class {
        private readonly IDbContextFactory<DbContext> _contextFactory;
        private readonly IMemoryCache _cache;

        public MemoryCacheRepositoryBase(IDbContextFactory<DbContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
            _contextFactory = contextFactory;
            _cache = cache;
        }

        public async Task<List<T>> MemoryCacheData() {
            try {
                var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
                return await _cache.GetOrCreateAsync(name, async entry => {
                    entry.SlidingExpiration = TimeSpan.FromMinutes(5);
                    await using (var concardContext = _contextFactory.CreateDbContext()) {
                        var dbSet = concardContext?.Set<T>();
                        if (dbSet is null) return null;
                        return await dbSet.AsNoTracking().ToListAsync();
                    }
                });
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return null;
        }

        public new async Task<bool> Insert(T entity, CancellationToken token) {
            var insert = await base.Insert(entity, token);
            if (insert) {
                var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
                _cache.Remove(name ?? string.Empty);
            }
            return insert;
        }

        public new void InsertAsync(T entity, CancellationToken token) {
            base.InsertAsync(entity, token);
            var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
            _cache.Remove(name ?? string.Empty);
        }

        public new async Task<bool> Update(T entity, CancellationToken token) {
            var update = await base.Update(entity, token);
            if (update) {
                var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
                _cache.Remove(name ?? string.Empty);
            }
            return update;
        }

        public new async Task<bool> Delete(T entity, CancellationToken token) {
            var delete = await base.Delete(entity, token);
            if (delete) {
                var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
                _cache.Remove(name ?? string.Empty);
            }
            return delete;
        }

        public new async Task<int> DeleteCount(int count, CancellationToken token) {
            var deleteCount = await base.DeleteCount(count, token);
            if (deleteCount > 0) {
                var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
                _cache.Remove(name ?? string.Empty);
            }

            return deleteCount;
        }

        public new async Task<int> DeleteCount(int count, Expression<Func<T, bool>> @where, CancellationToken token) {
            var deleteCount = await base.DeleteCount(count, @where, token);
            if (deleteCount > 0) {
                var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
                _cache.Remove(name ?? string.Empty);
            }

            return deleteCount;
        }

        public new async Task<bool> InsertOrUpdate(T entity, CancellationToken token) {
            var insertOrUpdate = await base.InsertOrUpdate(entity, token);
            if (insertOrUpdate) {
                var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
                _cache.Remove(name ?? string.Empty);
            }
            return insertOrUpdate;
        }

        public new async Task<bool> SyncEntities([NotNull] List<T> entities, CancellationToken token) {
            var syncEntities = await base.SyncEntities(entities, token);
            if (syncEntities) {
                var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
                _cache.Remove(name ?? string.Empty);
            }
            return syncEntities;
        }
    }
}