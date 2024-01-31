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

    public class RepositoryBase<T> : IRepository<T> where T : class {
        public readonly IDbContextFactory<DbContext> _contextFactory;
        private static SemaphoreSlim _transactionSlim = new(1);

        public RepositoryBase(IDbContextFactory<DbContext> contextFactory, IMemoryCache cache) {
            if (contextFactory == null || cache == null) {
                return;
            }
            _contextFactory = contextFactory;
        }

        public async Task<int> ExecuteSqlAsync(string sql, CancellationToken token) {
            if (sql.Equals(string.Empty)) return 0;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                return await concardContext.Database.ExecuteSqlRawAsync(sql, token);
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

            return 0;
        }

        public async Task<List<T>> FromSqlRaw(string sql, CancellationToken token) {
            if (sql.Equals(string.Empty)) return null;

            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                {
                    var dbSet = concardContext?.Set<T>();
                    if (dbSet is null) return null;
                    return await dbSet.FromSqlRaw(sql).ToListAsync(cancellationToken: token);
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
            return null;
        }

        public async Task<bool> Insert(T entity, CancellationToken token) {
            /*try {
                await using var concardContext = _contextFactory.CreateDbContext();
                {
                    var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
                    if (!string.IsNullOrEmpty(name)) {
                        var columns = new List<string>();
                        var values = new List<string>();
                        var sqlParameters = new List<SqlParameter>();
                        var propertyInfos = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

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
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var dbSet = concardContext?.Set<T>();
                            await dbSet.AddAsync(entity, token);
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
                //await contextTransaction?.RollbackAsync(token)!;
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return false;
        }

        public async void InsertAsync(T entity, CancellationToken token) {
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                {
                    var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
                    if (string.IsNullOrEmpty(name)) return;
                    var columns = new List<string>();
                    var values = new List<string>();
                    var sqlParameters = new List<SqlParameter>();
                    var propertyInfos = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

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
        }

        public async Task<bool> InsertRange(List<T> entitylist, CancellationToken token) {
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
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
            }
            catch (TaskCanceledException) {
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
                }
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
            }
            catch (Exception e) {
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
            }

            return false;
        }

        public async Task<bool> InsertOrUpdate(T entity, CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                var propertyInfos = typeof(T).GetProperties(
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

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
            catch (TransactionException e) {
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            catch (TaskCanceledException) {
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
                }
            }
            catch (Exception e) {
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return false;
        }

        public async Task<bool> InsertOrUpdateRange(List<T> entities,
            CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await _transactionSlim.WaitAsync(token);
                var propertyInfos = typeof(T).GetProperties(
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                await using (var concardContext = _contextFactory.CreateDbContext()) {
                    var strategy = concardContext.Database.CreateExecutionStrategy();
                    return await strategy.ExecuteAsync(async () => {
                        using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                            if (contextTransaction is not null) {
                                await concardContext.BulkInsertOrUpdateAsync(entities, new BulkConfig() {
                                    UseTempDB = true,
                                    UniqueTableNameTempDb = false,
                                    PropertiesToIncludeOnUpdate = propertyInfos?.Where(
                                        w => w.GetCustomAttribute<InsertOrUpdataAttribute>() != null
                                    )?.Select(s => s.Name)?.ToList(),
                                    /*PropertiesToExcludeOnUpdate = propertyInfos
                                        ?.Where(w => w.GetCustomAttribute<DatabaseGeneratedAttribute>() != null ||
                                                     w.GetCustomAttribute<ExcludeOnUpdateAttribute>() != null)
                                        ?.Select(s => s.Name)?.ToList(),*/
                                    /*PropertiesToInclude = propertyInfos?.Where(
                                        w => w.GetCustomAttribute<InsertOrUpdataAttribute>() != null)?.Select(s => s.Name)?.ToList(),*/

                                    /*PropertiesToExclude = propertyInfos
                                        ?.Where(w => w.GetCustomAttribute<InsertOrUpdataAttribute>() == null)
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
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            catch (TaskCanceledException) {
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
                }
            }
            catch (Exception e) {
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            finally {
                _transactionSlim.Release();
            }
            return false;
        }

        public async Task<bool> InsertOrUpdate(T entity, Expression<Func<T, object>> excludeColumns, CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                var exclude = new List<string>();
                var memberInfos = ((NewExpression)excludeColumns.Body).Members;
                if (memberInfos is not null) {
                    exclude = memberInfos.Select(p => p.Name).ToList();
                }

                var propertyInfos = typeof(T).GetProperties(
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
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
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            catch (TaskCanceledException) {
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
                }
            }
            catch (Exception e) {
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
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
                    exclude = memberInfos.Select(p => p.Name).ToList();
                }
                await _transactionSlim.WaitAsync(token);
                var propertyInfos = typeof(T).GetProperties(
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

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
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            catch (TaskCanceledException) {
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
                }
            }
            catch (Exception e) {
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            finally {
                _transactionSlim.Release();
            }
            return false;
        }

        public async Task<bool> Update(T entity, CancellationToken token) {
            /*try {
                await using var concardContext = _contextFactory.CreateDbContext();
                {
                    var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
                    if (!string.IsNullOrEmpty(name)) {
                        var sqlParameters = new List<SqlParameter>();
                        var propertyInfos = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

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
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
            }
            catch (TaskCanceledException) {
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
                }
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
            }
            catch (Exception e) {
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
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
                    exclude = memberInfos.Select(p => p.Name).ToList();
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
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
            }
            catch (TaskCanceledException) {
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
                }
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
            }
            catch (Exception e) {
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
            }

            return false;
        }

        public async Task<bool> Delete(T entity, CancellationToken token) {
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                {
                    var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
                    var propertyInfos = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
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

        public async Task<bool> DeleteRange(List<T> entities, CancellationToken token) {
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

        public async Task<int> DeleteCount(int count, CancellationToken token) {
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

        public async Task<int> DeleteCount(int count, Expression<Func<T, bool>> @where, CancellationToken token) {
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                {
                    var name = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
                    var dbSet = concardContext?.Set<T>();
                    if (dbSet is null) return 0;

                    return await dbSet.Where(where).Take(count).BatchDeleteAsync(cancellationToken: token);
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

        public async Task<List<T>> Select(Expression<Func<T, bool>> @where, int pageIndex, int pageSize, CancellationToken token) {
            pageIndex = pageIndex < 0 ? 0 : pageIndex;
            pageSize = pageSize > 1000 ? 1000 : pageSize;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<T>();
                if (dbSet is null) return null;
                return await dbSet.AsNoTracking()?.Where(where)
                    ?.Skip(pageIndex * pageSize)?.Take(pageSize)?.ToListAsync(token);
            }
            catch (Win32Exception) {
            }
            catch (TaskCanceledException) {
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return null;
        }

        public async Task<List<T>> Select<TOrder>(Expression<Func<T, bool>> @where, Expression<Func<T, TOrder>> order, int pageIndex, int pageSize, CancellationToken token) {
            pageIndex = pageIndex < 0 ? 0 : pageIndex;
            pageSize = pageSize > 1000 ? 1000 : pageSize;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<T>();
                if (dbSet is null) return null;
                return await dbSet.AsNoTracking()?.Where(where)?.OrderBy(order)
                    ?.Skip(pageIndex * pageSize)?.Take(pageSize)?.ToListAsync(token);
            }
            catch (Win32Exception) {
            }
            catch (TaskCanceledException) {
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return null;
        }

        public async Task<List<T>> Select<TOrder>(Expression<Func<T, bool>> where, Expression<Func<T, TOrder>> order, CancellationToken token) {
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<T>();
                if (dbSet is null) return null;
                return await dbSet.AsNoTracking()?.Where(where)?.OrderBy(order)
                    ?.ToListAsync(token);
            }
            catch (Win32Exception) {
            }
            catch (TaskCanceledException) {
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return null;
        }

        public async Task<List<T>> SelectOrderByDescending<TOrder>(Expression<Func<T, bool>> @where, Expression<Func<T, TOrder>> order, int pageIndex, int pageSize,
            CancellationToken token) {
            pageIndex = pageIndex < 0 ? 0 : pageIndex;
            //pageSize = pageSize > 1000 ? 1000 : pageSize;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<T>();
                if (dbSet is null) return null;
                return await dbSet.AsNoTracking()?.Where(where)?.OrderByDescending(order)
                    ?.Skip(pageIndex * pageSize)?.Take(pageSize)?.ToListAsync(token);
            }
            catch (Win32Exception) {
            }
            catch (TaskCanceledException) {
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return null;
        }

        public async Task<List<T>> SelectOrderByDescending<TOrder>(Expression<Func<T, bool>> where, Expression<Func<T, TOrder>> order, CancellationToken token) {
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<T>();
                if (dbSet is null) return null;
                return await dbSet.AsNoTracking()?.Where(where)?.OrderByDescending(order)
                    .ToListAsync(token);
            }
            catch (Win32Exception) {
            }
            catch (TaskCanceledException) {
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return null;
        }

        public async Task<T> FirstOrDefault(Expression<Func<T, bool>> @where, CancellationToken token) {
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<T>();
                if (dbSet is null) return null;
                return await dbSet.AsNoTracking().Where(where).FirstOrDefaultAsync(token);
            }
            catch (Win32Exception) {
            }
            catch (TaskCanceledException) {
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return null;
        }

        public async Task<int> Total(Expression<Func<T, bool>> @where, CancellationToken token) {
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<T>();
                if (dbSet is null) return 0;
                return await dbSet.AsNoTracking().Where(where).CountAsync(token);
            }
            catch (Win32Exception) {
            }
            catch (TaskCanceledException) {
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }

            return 0;
        }

        public async Task<bool> SyncEntities(List<T> entities, CancellationToken token) {
            IDbContextTransaction? contextTransaction = null;
            try {
                var propertyInfos = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance |
                                                            System.Reflection.BindingFlags.Public);
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
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
            }
            catch (TaskCanceledException) {
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
            }
            catch (SqlException e) {
                if (e.Number != -2) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
                }
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
            }
            catch (Exception e) {
                contextTransaction?.RollbackAsync(token).ConfigureAwait(false);
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"[{typeof(T).Name}]{e}");
            }

            return false;
        }
    }
}