using NLog;
using System.Reflection;
using System.Transactions;
using System.ComponentModel;
using EFCore.BulkExtensions;
using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using JayTom.Dws.Data.Attributes;
using JayTom.Dws.Domain.Repository;
using JayTom.Dws.Data.VideoApiData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore.Storage;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Infrastructure.Repository {

    public class RepositoryBase<T, TContext> : RepositoryContextBase<TContext>, IRepository<T>
        where T : class
        where TContext : DbContext {
        private static readonly SemaphoreSlim _transactionSlim = new(1);
        private static readonly PropertyInfo[] EntityProperties =
            typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        public RepositoryBase(IDbContextFactory<TContext> contextFactory, IMemoryCache cache)
            : base(contextFactory, cache) {
        }

        public virtual async Task<int> ExecuteSqlAsync(string sql, CancellationToken token) {
            ArgumentException.ThrowIfNullOrWhiteSpace(sql);
            await using var concardContext = _contextFactory.CreateDbContext();
            return await concardContext.Database.ExecuteSqlRawAsync(sql, token);
        }

        public async Task<List<T>> FromSqlRaw(string sql, CancellationToken token) {
            ArgumentException.ThrowIfNullOrWhiteSpace(sql);
            await using var concardContext = _contextFactory.CreateDbContext();
            return await concardContext.Set<T>().FromSqlRaw(sql).AsNoTracking()
                .ToListAsync(cancellationToken: token);
        }

        public virtual async Task<bool> Insert(T entity, CancellationToken token) {
            /*try {
                await using var concardContext = _contextFactory.CreateDbContext();
                {
                    var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
                    if (!string.IsNullOrEmpty(name)) {
                        var columns = new List<string>();
                        var values = new List<string>();
                        var sqlParameters = new List<SqlParameter>();
                        var propertyInfos = EntityProperties;

                        var list = propertyInfos?.Where(w => w.GetCustomAttribute<ColumnAttribute>() != null &&
                                                             w.GetCustomAttribute<DatabaseGeneratedAttribute>() == null)?.ToList();
                        if (list?.Any() == true) {
                            columns.AddRange(list.Select(s => $"[{s.Name}]"));
                            values.AddRange(list.Select(s => $"@{s.Name}vaule"));
                            sqlParameters.AddRange(list.Select(s => new SqlParameter($"@{s.Name}vaule", s.GetValue(entity, null) ?? DBNull.Value)));
                            var sql = @$" INSERT INTO {name}
                                     ({string.Join(",", columns)})
                                     VALUES ({string.Join(",", values)})";
                            var executeSqlRawAsync = await concardContext.Database.ExecuteSqlRawAsync(sql, sqlParameters.ToArray(), token);

                            return executeSqlRawAsync > 0;
                        }
                    }
                }
            }
            catch (Win32Exception) {
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
                }
            }
            catch (TaskCanceledException) {
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return false;*/
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                concardContext.Set<T>().Add(entity);
                return await concardContext.SaveChangesAsync(token) > 0;
            }
            catch (Win32Exception) {
            }
            catch (TaskCanceledException) {
            }
            catch (Exception e) {
                //await contextTransaction?.RollbackAsync(token)!;
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return false;
        }

        public virtual Task<bool> InsertAsync(T entity, CancellationToken token) =>
            Insert(entity, token);

        public virtual async Task<bool> InsertRange(List<T> entitylist, CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            await concardContext.BulkInsertAsync(entitylist, new BulkConfig() {
                                UseTempDB = true,
                                UniqueTableNameTempDb = false,
                                PreserveInsertOrder = true,
                                SetOutputIdentity = false
                            }, type: typeof(T), cancellationToken: token);
                            await contextTransaction.CommitAsync(token);
                            return true;
                        }

                        return false;
                    }
                });
            }
            catch (Win32Exception) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
                }
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
            }
            catch (Exception e) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
            }

            return false;
        }

        public virtual async Task<bool> InsertOrUpdate(T entity, CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
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
                                w => w.GetCustomAttribute<InsertOrUpdateAttribute>() != null)?.Select(s => s.Name)?.ToList(),
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
            catch (TransactionException e) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            catch (TaskCanceledException) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
                }
            }
            catch (Exception e) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return false;
        }

        public virtual async Task<bool> InsertOrUpdateRange(List<T> entities,
            CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                using var semaphoreLease =
                    await global::JayTom.Dws.Infrastructure.SemaphoreLease.EnterAsync(_transactionSlim, token);
                var propertyInfos = EntityProperties;

                await using (var concardContext = _contextFactory.CreateDbContext()) {
                    var strategy = concardContext.Database.CreateExecutionStrategy();
                    return await strategy.ExecuteAsync(async () => {
                        using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                            if (contextTransaction is not null) {
                                await concardContext.BulkInsertOrUpdateAsync(entities, new BulkConfig() {
                                    UseTempDB = true,
                                    UniqueTableNameTempDb = false,
                                    PropertiesToIncludeOnUpdate = propertyInfos?.Where(
                                        w => w.GetCustomAttribute<InsertOrUpdateAttribute>() != null
                                    )?.Select(s => s.Name)?.ToList(),
                                    /*PropertiesToExcludeOnUpdate = propertyInfos
                                        ?.Where(w => w.GetCustomAttribute<DatabaseGeneratedAttribute>() != null ||
                                                     w.GetCustomAttribute<ExcludeOnUpdateAttribute>() != null)
                                        ?.Select(s => s.Name)?.ToList(),*/
                                    /*PropertiesToInclude = propertyInfos?.Where(
                                        w => w.GetCustomAttribute<InsertOrUpdateAttribute>() != null)?.Select(s => s.Name)?.ToList(),*/

                                    /*PropertiesToExclude = propertyInfos
                                        ?.Where(w => w.GetCustomAttribute<InsertOrUpdateAttribute>() == null)
                                        ?.Select(s => s.Name)?.ToList(),*/
                                    PropertiesToExclude = propertyInfos
                                        ?.Where(w => w.GetCustomAttribute<DatabaseGeneratedAttribute>() != null)
                                        ?.Select(s => s.Name)?.ToList(),
                                    UpdateByProperties = propertyInfos
                                        ?.Where(w => w.GetCustomAttribute<UpdateByAttribute>() != null)
                                        ?.Select(s => s.Name)?.ToList(),
                                    PreserveInsertOrder = true,
                                    SetOutputIdentity = false
                                }, type: typeof(T), cancellationToken: token);
                                await contextTransaction.CommitAsync(token);
                                return true;
                            }

                            return false;
                        }
                    });
                }
            }
            catch (TransactionException e) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            catch (TaskCanceledException) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
                }
            }
            catch (Exception e) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return false;
        }

        public virtual async Task<bool> InsertOrUpdate(T entity, Expression<Func<T, object>> excludeColumns, CancellationToken token) {
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
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            catch (TaskCanceledException) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
                }
            }
            catch (Exception e) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return false;
        }

        public virtual async Task<bool> InsertOrUpdateRange(List<T> entities, Expression<Func<T, object>> excludeColumns, CancellationToken token) {
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
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            catch (TaskCanceledException) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
                }
            }
            catch (Exception e) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return false;
        }

        public virtual async Task<bool> Update(T entity, CancellationToken token) {
            /*try {
                await using var concardContext = _contextFactory.CreateDbContext();
                {
                    var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
                    if (!string.IsNullOrEmpty(name)) {
                        var sqlParameters = new List<SqlParameter>();
                        var propertyInfos = EntityProperties;

                        var list = propertyInfos?.Where(w => w.GetCustomAttribute<ColumnAttribute>() != null &&
                                                             w.GetCustomAttribute<DatabaseGeneratedAttribute>() == null)?.ToList();
                        var info = propertyInfos?.FirstOrDefault(f =>
                            f.GetCustomAttribute<DatabaseGeneratedAttribute>() != null);

                        if (list?.Any() == true && info is not null) {
                            var pairs = list.Select(s => new KeyValuePair<string, string>(s.Name, $"@{s.Name}vaule"))
                                ?.ToArray();
                            sqlParameters.AddRange(list.Select(s => new SqlParameter($"@{s.Name}vaule", s.GetValue(entity, null) ?? DBNull.Value)));
                            var sql = @$" UPDATE {name} SET {string.Join(",", pairs.Select(s => new string($"[{s.Key}]={s.Value}")))}
                                     WHERE {info.Name}={info.GetValue(entity, null)}";
                            var executeSqlRawAsync = await concardContext.Database.ExecuteSqlRawAsync(sql, sqlParameters.ToArray(), token);

                            return executeSqlRawAsync > 0;
                        }
                    }
                }
            }
            catch (Win32Exception) {
            }
            catch (TaskCanceledException) {
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
                }
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return false;*/
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var dbSet = concardContext?.Set<T>();
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
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
            }
            catch (Exception e) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return false;
        }

        public virtual async Task<bool> UpdateRange(List<T> entities, CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            await concardContext.BulkUpdateAsync(entities, new BulkConfig() {
                                UseTempDB = true
                            }, type: typeof(T), cancellationToken: token);
                            await contextTransaction.CommitAsync(token);
                            return true;
                        }
                        return false;
                    }
                });
            }
            catch (Win32Exception) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
                }
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
            }
            catch (Exception e) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
            }

            return false;
        }

        public virtual async Task<bool> UpdateRange(List<T> entities, Expression<Func<T, object>> excludeColumns, CancellationToken token) {
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
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
                }
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
            }
            catch (Exception e) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
            }

            return false;
        }

        public virtual async Task<bool> Delete(T entity, CancellationToken token) {
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                {
                    var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
                    var propertyInfos = EntityProperties;
                    var info = propertyInfos?.FirstOrDefault(f =>
                        f.GetCustomAttribute<DatabaseGeneratedAttribute>() != null);
                    if (string.IsNullOrEmpty(name) ||
                        info is null) return false;
                    var sql = @$" DELETE FROM {name} WHERE {info.Name}={info?.GetValue(entity, null)}";
                    var executeSqlRawAsync = await concardContext.Database.ExecuteSqlRawAsync(sql, token);

                    return executeSqlRawAsync > 0;
                }
            }
            catch (Win32Exception) {
            }
            catch (TaskCanceledException) {
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
                }
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return false;
        }

        public virtual async Task<bool> DeleteRange(List<T> entities, CancellationToken token) {
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<T>();
                if (dbSet is not null) {
                    await concardContext.BulkDeleteAsync(entities, cancellationToken: token);
                    return true;
                }
            }
            catch (Win32Exception) {
            }
            catch (TaskCanceledException) {
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
                }
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return false;
        }

        public virtual async Task<int> DeleteCount(int count, CancellationToken token) {
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                {
                    var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;

                    var sql = @$" DELETE TOP ({count}) {name}";
                    var executeSqlRawAsync = await concardContext.Database.ExecuteSqlRawAsync(sql, token);

                    return executeSqlRawAsync;
                }
            }
            catch (Win32Exception) {
            }
            catch (TaskCanceledException) {
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
                }
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return 0;
        }

        public virtual async Task<int> DeleteCount(int count, Expression<Func<T, bool>> @where, CancellationToken token) {
            await using var concardContext = _contextFactory.CreateDbContext();
            return await concardContext.Set<T>()
                .Where(where)
                .Take(Math.Max(0, count))
                .ExecuteDeleteAsync(cancellationToken: token);
        }

        public async Task<List<T>> Select(Expression<Func<T, bool>> @where, int pageIndex, int pageSize, CancellationToken token) {
            pageIndex = pageIndex < 0 ? 0 : pageIndex;
            pageSize = Math.Clamp(pageSize, 1, 1000);
            pageIndex = Math.Min(pageIndex, int.MaxValue / pageSize);
            await using var concardContext = _contextFactory.CreateDbContext();
            return await concardContext.Set<T>().AsNoTracking().Where(where)
                .Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(token);
        }

        public async Task<List<T>> Select<TOrder>(Expression<Func<T, bool>> @where, Expression<Func<T, TOrder>> order, int pageIndex, int pageSize, CancellationToken token) {
            pageIndex = pageIndex < 0 ? 0 : pageIndex;
            pageSize = Math.Clamp(pageSize, 1, 1000);
            pageIndex = Math.Min(pageIndex, int.MaxValue / pageSize);
            await using var concardContext = _contextFactory.CreateDbContext();
            return await concardContext.Set<T>().AsNoTracking().Where(where).OrderBy(order)
                .Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(token);
        }

        public async Task<List<T>> Select<TOrder>(Expression<Func<T, bool>> where, Expression<Func<T, TOrder>> order, CancellationToken token) {
            await using var concardContext = _contextFactory.CreateDbContext();
            return await concardContext.Set<T>().AsNoTracking().Where(where).OrderBy(order)
                .ToListAsync(token);
        }

        public async Task<List<T>> SelectOrderByDescending<TOrder>(Expression<Func<T, bool>> @where, Expression<Func<T, TOrder>> order, int pageIndex, int pageSize,
            CancellationToken token) {
            pageIndex = pageIndex < 0 ? 0 : pageIndex;
            pageSize = Math.Clamp(pageSize, 1, 1000);
            pageIndex = Math.Min(pageIndex, int.MaxValue / pageSize);
            await using var concardContext = _contextFactory.CreateDbContext();
            return await concardContext.Set<T>().AsNoTracking().Where(where).OrderByDescending(order)
                .Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(token);
        }

        public async Task<List<T>> SelectOrderByDescending<TOrder>(Expression<Func<T, bool>> where, Expression<Func<T, TOrder>> order, CancellationToken token) {
            await using var concardContext = _contextFactory.CreateDbContext();
            return await concardContext.Set<T>().AsNoTracking().Where(where).OrderByDescending(order)
                .ToListAsync(token);
        }

        /// <summary>
        /// 在事务存在时等待回滚完成，避免事务释放与回滚并发。
        /// </summary>
        private static Task RollbackTransactionAsync(IDbContextTransaction? transaction, CancellationToken token) {
            return transaction?.RollbackAsync(token) ?? Task.CompletedTask;
        }

        public async Task<T?> FirstOrDefault(Expression<Func<T, bool>> @where, CancellationToken token) {
            await using var concardContext = _contextFactory.CreateDbContext();
            return await concardContext.Set<T>().AsNoTracking().FirstOrDefaultAsync(where, token);
        }

        public async Task<int> Total(Expression<Func<T, bool>> @where, CancellationToken token) {
            await using var concardContext = _contextFactory.CreateDbContext();
            return await concardContext.Set<T>().CountAsync(where, token);
        }

        public virtual async Task<bool> SyncEntities(List<T> entities, CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                var propertyInfos = EntityProperties;
                var excludeOnUpdateColumns = propertyInfos.Where(w => w.GetCustomAttribute<ExcludeOnUpdateAttribute>() != null ||
                                                                      w.GetCustomAttribute<DatabaseGeneratedAttribute>() != null)
                    .Select(s => s.Name)?.ToList();
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        await concardContext.BulkInsertOrUpdateOrDeleteAsync(entities, new BulkConfig() {
                            UseTempDB = true,
                            PropertiesToExcludeOnUpdate = excludeOnUpdateColumns
                        }, type: typeof(T), cancellationToken: token);
                        await contextTransaction.CommitAsync(token);
                        return true;
                    }
                });
            }
            catch (Win32Exception) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
                }
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
            }
            catch (Exception e) {
                await RollbackTransactionAsync(contextTransaction, token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
            }

            return false;
        }
    }
}
