using NLog;
using System.Reflection;
using System.Transactions;
using System.ComponentModel;
using EFCore.BulkExtensions;
using Microsoft.Data.Sqlite;
using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using JayTom.Dws.Data.Attributes;
using JayTom.Dws.Domain.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore.Storage;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Infrastructure.Repository {

    internal static class LocalDatabaseWriteCoordinator {
        /// <summary>
        /// 跨实体仓储共享的 SQLite 写入门。
        /// </summary>
        internal static readonly SemaphoreSlim Gate = new(1, 1);
    }

    public class LocalRepositoryBase<T> : IRepository<T> where T : class {
        protected IDbContextFactory<DbContext> _contextFactory;
        protected IMemoryCache _cache;
        private static readonly SemaphoreSlim _changeSlim = LocalDatabaseWriteCoordinator.Gate;
        private static readonly SemaphoreSlim _transactionSlim = LocalDatabaseWriteCoordinator.Gate;
        private static readonly PropertyInfo[] EntityProperties =
            typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        public LocalRepositoryBase(IDbContextFactory<DbContext> contextFactory, IMemoryCache cache) {
            ArgumentNullException.ThrowIfNull(contextFactory);
            ArgumentNullException.ThrowIfNull(cache);
            _contextFactory = contextFactory;
            _cache = cache;
        }

        public async Task<int> ExecuteSqlAsync(string sql, CancellationToken token) {
            if (sql.Equals(string.Empty)) return 0;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                return await concardContext.Database.ExecuteSqlRawAsync(sql, token);
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return 0;
        }

        public async Task<List<T>> FromSqlRaw(string sql, CancellationToken token) {
            if (string.IsNullOrWhiteSpace(sql)) return new List<T>();

            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                return await concardContext.Set<T>().FromSqlRaw(sql).AsNoTracking()
                    .ToListAsync(cancellationToken: token);
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return new List<T>();
        }

        public async Task<bool> Insert(T entity, CancellationToken token) {
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                EntityGraphWriteGuard.ClearDependentReferenceNavigations(concardContext, entity);
                concardContext.Set<T>().Add(entity);
                return await concardContext.SaveChangesAsync(token) > 0;
            }
            catch (Win32Exception) {
            }
            catch (TaskCanceledException) {
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return false;
        }

        public async Task InsertAsync(T entity, CancellationToken token) {
            await Insert(entity, token);
        }

        public async Task<bool> InsertRange(List<T> entitylist, CancellationToken token) {
            if (entitylist.Count == 0) {
                return true;
            }

            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                EntityGraphWriteGuard.ClearDependentReferenceNavigations(concardContext, entitylist);
                concardContext.Set<T>().AddRange(entitylist);
                return await concardContext.SaveChangesAsync(token) > 0;
            }
            catch (Win32Exception) {
            }
            catch (TaskCanceledException) {
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
                }
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
            }

            return false;
        }

        public async Task<bool> InsertOrUpdate(T entity, CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                using var semaphoreLease =
                    await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(_transactionSlim, token);
                var propertyInfos = EntityProperties;

                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        await concardContext.BulkInsertOrUpdateAsync(new List<T>() {
                            entity
                        }, new BulkConfig() {
                            UseTempDB = true,
                            UniqueTableNameTempDb = false,
                            PropertiesToIncludeOnUpdate = propertyInfos?.Where(
                                w => w.GetCustomAttribute<InsertOrUpdataAttribute>() != null)?.Select(s => s.Name)?.ToList(),
                            PropertiesToExclude = propertyInfos
                                ?.Where(w => w.GetCustomAttribute<DatabaseGeneratedAttribute>() != null ||
                                             w.GetCustomAttribute<ExcludeOnUpdateAttribute>() != null)
                                ?.Select(s => s.Name)?.ToList(),
                            PreserveInsertOrder = true,
                            SetOutputIdentity = false,
                            UpdateByProperties = propertyInfos
                                ?.Where(w => w.GetCustomAttribute<UpdateByAttribute>() != null)
                                ?.Select(s => s.Name)?.ToList(),
                        }, type: typeof(T), cancellationToken: token);
                        await contextTransaction.CommitAsync(token);
                        return true;
                    }
                });
            }
            catch (TaskCanceledException) {
                await TryRollbackAsync(contextTransaction);
            }
            catch (Exception e) {
                await TryRollbackAsync(contextTransaction);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return false;
        }

        public async Task<bool> InsertOrUpdateRange(List<T> entities,
            CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                using var semaphoreLease =
                    await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(_transactionSlim, token);
                var propertyInfos = EntityProperties;
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        await concardContext.BulkInsertOrUpdateAsync(entities, new BulkConfig() {
                            UseTempDB = true,
                            UniqueTableNameTempDb = false,
                            PropertiesToIncludeOnUpdate = propertyInfos?.Where(
                                w => w.GetCustomAttribute<InsertOrUpdataAttribute>() != null)?.Select(s => s.Name)?.ToList(),
                            UpdateByProperties = propertyInfos
                                ?.Where(w => w.GetCustomAttribute<UpdateByAttribute>() != null)
                                ?.Select(s => s.Name)?.ToList(),
                            PropertiesToExclude = propertyInfos
                                ?.Where(w => w.GetCustomAttribute<DatabaseGeneratedAttribute>() != null)
                                ?.Select(s => s.Name)?.ToList(),
                            PreserveInsertOrder = true,
                            SetOutputIdentity = false,
                        }, type: typeof(T), cancellationToken: token);
                        await contextTransaction.CommitAsync(token);
                        return true;
                    }
                });
            }
            catch (TaskCanceledException) {
                await TryRollbackAsync(contextTransaction);
            }
            catch (Exception e) {
                await TryRollbackAsync(contextTransaction);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return false;
        }

        public async Task<bool> InsertOrUpdate(T entity, Expression<Func<T, object>> excludeColumns, CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                var exclude = new List<string>();
                var memberInfos = ((NewExpression)excludeColumns.Body).Members;
                if (memberInfos is not null) {
                    exclude = [.. memberInfos.Select(p => p.Name)];
                }

                var propertyInfos = EntityProperties;
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        await concardContext.BulkInsertOrUpdateAsync(new List<T>() {
                            entity
                        }, new BulkConfig() {
                            UseTempDB = true,
                            UniqueTableNameTempDb = false,
                            PropertiesToExcludeOnUpdate = exclude,
                            PreserveInsertOrder = true,
                            SetOutputIdentity = false,
                            UpdateByProperties = propertyInfos
                                ?.Where(w => w.GetCustomAttribute<UpdateByAttribute>() != null)
                                ?.Select(s => s.Name)?.ToList(),
                        }, type: typeof(T), cancellationToken: token);
                        await contextTransaction.CommitAsync(token);
                        return true;
                    }
                });
            }
            catch (TransactionException e) {
                await TryRollbackAsync(contextTransaction);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            catch (TaskCanceledException) {
                await TryRollbackAsync(contextTransaction);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
                }
            }
            catch (Exception e) {
                await TryRollbackAsync(contextTransaction);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return false;
        }

        public async Task<bool> InsertOrUpdateRange(List<T> entities, Expression<Func<T, object>> excludeColumns, CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                var exclude = new List<string>();
                var memberInfos = ((NewExpression)excludeColumns.Body).Members;
                if (memberInfos is not null) {
                    exclude = [.. memberInfos.Select(p => p.Name)];
                }
                using var semaphoreLease =
                    await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(_transactionSlim, token);
                var propertyInfos = EntityProperties;

                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is null) return false;
                        await concardContext.BulkInsertOrUpdateAsync(entities, new BulkConfig() {
                            UseTempDB = true,
                            UniqueTableNameTempDb = false,
                            PropertiesToExcludeOnUpdate = exclude,
                            UpdateByProperties = propertyInfos
                                ?.Where(w => w.GetCustomAttribute<UpdateByAttribute>() != null)
                                ?.Select(s => s.Name)?.ToList(),
                            PreserveInsertOrder = true,
                            SetOutputIdentity = false
                        }, type: typeof(T), cancellationToken: token);
                        await contextTransaction.CommitAsync(token);
                        return true;
                    }
                });
            }
            catch (TransactionException e) {
                await TryRollbackAsync(contextTransaction);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            catch (TaskCanceledException) {
                await TryRollbackAsync(contextTransaction);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
                }
            }
            catch (Exception e) {
                await TryRollbackAsync(contextTransaction);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return false;
        }

        public async Task<bool> Update(T entity, CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var dbSet = concardContext?.Set<T>();
                            EntityGraphWriteGuard.ClearDependentReferenceNavigations(concardContext, entity);
                            dbSet.Update(entity);
                            await concardContext?.SaveChangesAsync(token);
                            await contextTransaction.CommitAsync(token);

                            return true;
                        }
                    }

                    return false;
                });
            }
            catch (Win32Exception) {
                await contextTransaction?.RollbackAsync(token)!;
            }
            catch (TaskCanceledException) {
                await contextTransaction?.RollbackAsync(token)!;
            }
            catch (Exception e) {
                await contextTransaction?.RollbackAsync(token)!;
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return false;
        }

        public async Task<bool> UpdateRange(List<T> entities, CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                using var semaphoreLease =
                    await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(_transactionSlim, token);
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        var dbSet = concardContext?.Set<T>();
                        if (dbSet is null) return true;
                        await concardContext.BulkUpdateAsync(entities, new BulkConfig() {
                            UseTempDB = true
                        }, cancellationToken: token);
                        await contextTransaction.CommitAsync(token);
                    }
                    return true;
                });
            }
            catch (Win32Exception) {
                await TryRollbackAsync(contextTransaction);
            }
            catch (TaskCanceledException) {
                await TryRollbackAsync(contextTransaction);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
                }
                await TryRollbackAsync(contextTransaction);
            }
            catch (Exception e) {
                await TryRollbackAsync(contextTransaction);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
            }
            return false;
        }

        public async Task<bool> UpdateRange(List<T> entities, Expression<Func<T, object>> excludeColumns, CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                var exclude = new List<string>();
                var memberInfos = ((NewExpression)excludeColumns.Body).Members;
                if (memberInfos is not null) {
                    exclude = [.. memberInfos.Select(p => p.Name)];
                }

                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is null) return false;
                        await concardContext.BulkUpdateAsync(entities, new BulkConfig() {
                            UseTempDB = true,
                            PropertiesToExcludeOnUpdate = exclude,
                        }, type: typeof(T), cancellationToken: token);
                        await contextTransaction.CommitAsync(token);
                        return true;
                    }
                });
            }
            catch (Win32Exception) {
                await TryRollbackAsync(contextTransaction);
            }
            catch (TaskCanceledException) {
                await TryRollbackAsync(contextTransaction);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
                }
                await TryRollbackAsync(contextTransaction);
            }
            catch (Exception e) {
                await TryRollbackAsync(contextTransaction);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
            }

            return false;
        }

        public async Task<bool> Delete(T entity, CancellationToken token) {
            try {
                using var semaphoreLease =
                    await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(_changeSlim, token);
                await using var concardContext = _contextFactory.CreateDbContext();
                var entry = concardContext.Entry(entity);
                if (entry.State == EntityState.Detached) {
                    concardContext.Set<T>().Attach(entity);
                }
                concardContext.Set<T>().Remove(entity);
                return await concardContext.SaveChangesAsync(token) > 0;
            }
            catch (Win32Exception) { }
            catch (TaskCanceledException) { }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return false;
        }

        public async Task<bool> DeleteRange(List<T> entities, CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                using var semaphoreLease =
                    await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(_transactionSlim, token);
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                var dbSet = concardContext?.Set<T>();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        await concardContext.BulkDeleteAsync(entities, cancellationToken: token);
                        await contextTransaction.CommitAsync(token);
                        return true;
                    }
                });
            }
            catch (Win32Exception) {
                await TryRollbackAsync(contextTransaction);
            }
            catch (TaskCanceledException) {
                await TryRollbackAsync(contextTransaction);
            }
            catch (Exception e) {
                await TryRollbackAsync(contextTransaction);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return false;
        }

        public async Task<int> DeleteCount(int count, CancellationToken token) {
            if (count <= 0) {
                return 0;
            }
            try {
                using var semaphoreLease =
                    await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(_changeSlim, token);
                await using var concardContext = _contextFactory.CreateDbContext();
                {
                    var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;

                    var sql = $@"Delete from {name} where rowid IN (Select rowid from {name} limit {count})";
                    var executeSqlRawAsync = await concardContext.Database.ExecuteSqlRawAsync(sql, token);

                    return executeSqlRawAsync;
                }
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return 0;
        }

        public async Task<int> DeleteCount(int count, Expression<Func<T, bool>> @where, CancellationToken token) {
            try {
                using var semaphoreLease =
                    await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(_changeSlim, token);
                await using var concardContext = _contextFactory.CreateDbContext();
                {
                    var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
                    var dbSet = concardContext?.Set<T>();
                    if (dbSet is null) return 0;

                    var itemsToDelete = await dbSet.Where(where).Take(count).ToListAsync(cancellationToken: token);
                    dbSet.RemoveRange(itemsToDelete);
                    return await concardContext?.SaveChangesAsync(cancellationToken: token);

                    //return await dbSet.Where(where).Where((item, index) => index < count).BatchDeleteAsync(cancellationToken: token);
                    // return await dbSet.Where(where).Take(count).BatchDeleteAsync(cancellationToken: token);
                }
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return 0;
        }

        public async Task<List<T>> Select(Expression<Func<T, bool>> @where, int pageIndex, int pageSize, CancellationToken token) {
            pageIndex = pageIndex < 0 ? 0 : pageIndex;
            pageSize = pageSize > 1000 ? 1000 : pageSize;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                return await concardContext.Set<T>().AsNoTracking().Where(where)
                    .Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(token);
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return new List<T>();
        }

        public async Task<List<T>> Select<TOrder>(Expression<Func<T, bool>> @where, Expression<Func<T, TOrder>> order, int pageIndex, int pageSize, CancellationToken token) {
            pageIndex = pageIndex < 0 ? 0 : pageIndex;
            pageSize = pageSize > 1000 ? 1000 : pageSize;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                return await concardContext.Set<T>().AsNoTracking().Where(where).OrderBy(order)
                    .Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(token);
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return new List<T>();
        }

        public async Task<List<T>> Select<TOrder>(Expression<Func<T, bool>> where, Expression<Func<T, TOrder>> order, CancellationToken token) {
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                return await concardContext.Set<T>().AsNoTracking().Where(where).OrderBy(order)
                    .ToListAsync(token);
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return new List<T>();
        }

        public async Task<List<T>> SelectOrderByDescending<TOrder>(Expression<Func<T, bool>> @where, Expression<Func<T, TOrder>> order, int pageIndex, int pageSize,
            CancellationToken token) {
            pageIndex = pageIndex < 0 ? 0 : pageIndex;
            pageSize = pageSize > 1000 ? 1000 : pageSize;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                return await concardContext.Set<T>().AsNoTracking().Where(where).OrderByDescending(order)
                    .Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(token);
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return new List<T>();
        }

        public async Task<List<T>> SelectOrderByDescending<TOrder>(Expression<Func<T, bool>> where, Expression<Func<T, TOrder>> order, CancellationToken token) {
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                return await concardContext.Set<T>().AsNoTracking().Where(where).OrderByDescending(order)
                    .ToListAsync(token);
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return new List<T>();
        }

        public async Task<T?> FirstOrDefault(Expression<Func<T, bool>> @where, CancellationToken token) {
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                return await concardContext.Set<T>().AsNoTracking().FirstOrDefaultAsync(where, token);
            }
            catch (NullReferenceException ex) {
                // 记录详细的错误信息
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"NullReferenceException: {ex.Message}");
                throw;
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return null;
        }

        public async Task<int> Total(Expression<Func<T, bool>> @where, CancellationToken token) {
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                return await concardContext.Set<T>().CountAsync(where, token);
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return 0;
        }

        public async Task<bool> SyncEntities(List<T> entities, CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                var propertyInfos = EntityProperties;
                var excludeOnUpdateColumns = propertyInfos.Where(w => w.GetCustomAttribute<ExcludeOnUpdateAttribute>() != null)
                    .Select(s => s.Name)?.ToList();
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        EntityGraphWriteGuard.ClearDependentReferenceNavigations(concardContext, entities);
                        var dbSet = concardContext.Set<T>();
                        var primaryKeyProperty =
                            EntityGraphWriteGuard.GetSinglePrimaryKeyProperty(concardContext, typeof(T));
                        if (primaryKeyProperty is null) {
                            await concardContext.BulkInsertOrUpdateOrDeleteAsync(entities, new BulkConfig() {
                                UseTempDB = true,
                                PropertiesToExcludeOnUpdate = excludeOnUpdateColumns
                            }, type: typeof(T), cancellationToken: token);
                            await contextTransaction.CommitAsync(token);
                            return true;
                        }

                        // 云端同步按主键比较，避免引用比较导致“全删再全加”。
                        var existingEntities = await dbSet.AsNoTracking().ToListAsync(token);
                        var incomingEntitiesByKey = entities
                            .Where(entity => !EntityGraphWriteGuard.IsDefaultPrimaryKeyValue(
                                EntityGraphWriteGuard.GetPrimaryKeyValue(entity, primaryKeyProperty),
                                primaryKeyProperty))
                            .GroupBy(entity => EntityGraphWriteGuard.GetPrimaryKeyValue(entity, primaryKeyProperty))
                            .Where(group => group.Key is not null)
                            .ToDictionary(group => group.Key!, group => group.Last());
                        var incomingKeys = incomingEntitiesByKey.Keys.ToHashSet();
                        var existingKeys = existingEntities
                            .Select(entity => EntityGraphWriteGuard.GetPrimaryKeyValue(entity, primaryKeyProperty))
                            .Where(key => key is not null)
                            .Select(key => key!)
                            .ToHashSet();

                        var entitiesToDelete = existingEntities
                            .Where(entity => {
                                var key = EntityGraphWriteGuard.GetPrimaryKeyValue(entity, primaryKeyProperty);
                                return key is not null && !incomingKeys.Contains(key);
                            })
                            .ToList();
                        if (entitiesToDelete.Count > 0) {
                            concardContext.RemoveRange(entitiesToDelete);
                            await concardContext.SaveChangesAsync(token);
                        }

                        var entitiesToAdd = entities
                            .Where(entity => {
                                var key = EntityGraphWriteGuard.GetPrimaryKeyValue(entity, primaryKeyProperty);
                                return EntityGraphWriteGuard.IsDefaultPrimaryKeyValue(key, primaryKeyProperty) ||
                                       key is not null && !existingKeys.Contains(key);
                            })
                            .ToList();
                        if (entitiesToAdd.Count > 0) {
                            concardContext.AddRange(entitiesToAdd);
                        }

                        var entitiesToUpdate = incomingEntitiesByKey
                            .Where(pair => existingKeys.Contains(pair.Key))
                            .Select(pair => pair.Value)
                            .ToList();
                        foreach (var entity in entitiesToUpdate) {
                            concardContext.Update(entity);
                            foreach (var property in excludeOnUpdateColumns) {
                                concardContext.Entry(entity).Property(property).IsModified = false;
                            }
                        }

                        await concardContext.SaveChangesAsync(token);
                        await contextTransaction.CommitAsync(token);

                        return true;
                    }
                });
            }
            catch (Win32Exception) {
                await TryRollbackAsync(contextTransaction);
            }
            catch (TaskCanceledException) {
                await TryRollbackAsync(contextTransaction);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
                }
                await TryRollbackAsync(contextTransaction);
            }
            catch (Exception e) {
                await TryRollbackAsync(contextTransaction);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
            }

            return false;
        }

        /// <summary>
        /// 尽力回滚事务并记录回滚阶段的异常。
        /// </summary>
        private static async Task TryRollbackAsync(IDbContextTransaction? transaction) {
            if (transaction is null) {
                return;
            }

            try {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception exception) {
                LogManager.GetCurrentClassLogger()
                    .Log(LogLevel.Error, $"回滚本地事务失败:{exception}");
            }
        }
    }
}
