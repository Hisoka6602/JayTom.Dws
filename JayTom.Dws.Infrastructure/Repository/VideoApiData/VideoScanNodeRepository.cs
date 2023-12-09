using NLog;
using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Data.VideoApiData;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore.Storage;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Repository.VideoApiData;

namespace JayTom.Dws.Infrastructure.Repository.VideoApiData {

    public class VideoScanNodeRepository : RepositoryBase<VideoScanNodeInfoModel>, IVideoScanNodeRepository {

        public VideoScanNodeRepository(IDbContextFactory<VideoApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<KeyValuePair<bool, List<string>>> GroupedNodeNames(CancellationToken token = default) {
            try {
                await using (var concardContext = _contextFactory.CreateDbContext()) {
                    var dbSet = concardContext?.Set<VideoScanNodeInfoModel>();
                    if (dbSet is null) return new KeyValuePair<bool, List<string>>(false, new List<string>());

                    var listAsync = await dbSet.AsNoTracking()
                        ?.GroupBy(g => g.Name)
                        ?.Select(s => s.Key)
                        ?.ToListAsync(cancellationToken: token)!;

                    return new KeyValuePair<bool, List<string>>(true, listAsync);
                }
            }
            catch (Win32Exception) {
            }
            catch (TaskCanceledException) {
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
                return new KeyValuePair<bool, List<string>>(false, new List<string>());
            }
            return new KeyValuePair<bool, List<string>>(false, new List<string>());
        }

        public new async Task<bool> InsertOrUpdate([NotNull] VideoScanNodeInfoModel entity, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var videoScanNodeInfoModels = concardContext?.Set<VideoScanNodeInfoModel>();

                            var existingEntity = videoScanNodeInfoModels.FirstOrDefault(a =>
                                a.BarcodeId.Equals(entity.BarcodeId) && a.Name.Equals(entity.Name));

                            if (existingEntity != null) {
                                // 更新现有实体的属性
                                concardContext.Entry(existingEntity).CurrentValues.SetValues(entity);
                            }
                            else {
                                // 添加新实体
                                await videoScanNodeInfoModels.AddAsync(entity, token);
                            }

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