using NLog;
using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore.Storage;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.ConnectionParams;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig.ConnectionParams {
    public class CommunicationConnectionConfigRepository : LocalRepositoryBase<CommunicationConnectionConfigInfoModel, SqliteConfContext>, ICommunicationConnectionConfigRepository {

        public CommunicationConnectionConfigRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<List<CommunicationConnectionConfigInfoModel>> CommunicationConnectionConfigItems(Expression<Func<CommunicationConnectionConfigInfoModel, bool>> where, CancellationToken token = default) {
            try {
                await using (var concardContext = _contextFactory.CreateDbContext()) {
                    var dbSet = concardContext?.Set<CommunicationConnectionConfigInfoModel>();
                    if (dbSet is null) return new List<CommunicationConnectionConfigInfoModel>();
                    return await dbSet.AsNoTracking()
                        .Where(where)
                        .OrderByDescending(o => o.CreateTime)
                        .Include(b => b.DeviceExtensionConfigInfo)
                        .Include(b => b.TcpConnectionConfigInfo)
                        .ThenInclude(c => c.TcpConfigItems)
                        .Include(b => b.HeartbeatConfigInfo)
                        .Include(b => b.SerialPortConfigInfo)

                        .ToListAsync(cancellationToken: token);
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error(e);
                return new List<CommunicationConnectionConfigInfoModel>();
            }
        }

        public async Task<bool> InsertDetailAsync(CommunicationConnectionConfigInfoModel entity, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var communicationConnectionConfigInfoModels = concardContext?.Set<CommunicationConnectionConfigInfoModel>();
                            EntityGraphWriteGuard.ClearDependentReferenceNavigations(concardContext, entity);
                            var entityEntry = await communicationConnectionConfigInfoModels.AddAsync(entity, token);
                            var saveChangesAsync = await concardContext?.SaveChangesAsync(token);
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

        public async Task<bool> InsertRangeDetailAsync(List<CommunicationConnectionConfigInfoModel> entities, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var communicationConnectionConfigInfoModels = concardContext?.Set<CommunicationConnectionConfigInfoModel>();
                            EntityGraphWriteGuard.ClearDependentReferenceNavigations(concardContext, entities);
                            await communicationConnectionConfigInfoModels.AddRangeAsync(entities, token);
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

        public async Task<bool> UpdateDetailAsync(CommunicationConnectionConfigInfoModel entity, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var communicationConnectionConfigInfoModels = concardContext?.Set<CommunicationConnectionConfigInfoModel>();
                            var serialPortConfigInfoModels = concardContext?.Set<SerialPortConfigInfoModel>();
                            var tcpConnectionConfigInfoModels = concardContext?.Set<TcpConnectionConfigInfoModel>();
                            var deviceExtensionConfigInfoModels = concardContext?.Set<DeviceExtensionConfigInfoModel>();
                            var heartbeatConfigInfoModels = concardContext?.Set<HeartbeatConfigInfoModel>();

                            // 先删除原有的
                            var portConfigInfoModels = serialPortConfigInfoModels?.AsTracking()?.Where(w => w.CommunicationConnectionId.Equals(entity.Id))
                                ?.ToList() ?? new List<SerialPortConfigInfoModel>();
                            var connectionConfigInfoModels = tcpConnectionConfigInfoModels?.AsTracking()?.Where(w => w.CommunicationConnectionId.Equals(entity.Id))
                                ?.ToList() ?? new List<TcpConnectionConfigInfoModel>();
                            var extensionConfigInfoModels = deviceExtensionConfigInfoModels?.AsTracking()
                                ?.Where(w => w.CommunicationConnectionId.Equals(entity.Id))
                                ?.ToList() ?? new List<DeviceExtensionConfigInfoModel>();
                            var configInfoModels = heartbeatConfigInfoModels?.AsTracking()?.Where(w => w.CommunicationConnectionId.Equals(entity.Id))
                                ?.ToList() ?? new List<HeartbeatConfigInfoModel>();

                            concardContext.RemoveRange(portConfigInfoModels);
                            concardContext.RemoveRange(connectionConfigInfoModels);
                            concardContext.RemoveRange(extensionConfigInfoModels);
                            concardContext.RemoveRange(configInfoModels);
                            EntityGraphWriteGuard.ClearDependentReferenceNavigations(concardContext, entity);
                            communicationConnectionConfigInfoModels.Update(entity);
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
    }
}
