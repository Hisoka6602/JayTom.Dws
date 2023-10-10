using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig {

    public class SortingInstructionBindingRepository : LocalRepositoryBase<SortingInstructionBindingInfoModel>, ISortingInstructionBindingRepository {

        public SortingInstructionBindingRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<List<SortingInstructionBindingInfoModel>> InstructionBindings(Expression<Func<SortingInstructionBindingInfoModel, bool>> where, CancellationToken token = default) {
            try {
                await using (var concardContext = _contextFactory.CreateDbContext()) {
                    var dbSet = concardContext?.Set<SortingInstructionBindingInfoModel>();
                    if (dbSet is null) return new List<SortingInstructionBindingInfoModel>();
                    return await dbSet.AsNoTracking()
                        .Where(where)
                        .OrderByDescending(o => o.CreateTime)
                        .Include(b => b.InstructionItems)
                        .AsSingleQuery() // 添加AsSingleQuery
                        .Select(b => new SortingInstructionBindingInfoModel {
                            Remarks = b.Remarks,
                            CreateTime = b.CreateTime,
                            ExitId = b.ExitId,
                            Id = b.Id,
                            DelaySendMilliseconds = b.DelaySendMilliseconds,
                            SendIntervalMilliseconds = b.SendIntervalMilliseconds,
                            IsActive = b.IsActive,
                            ModifyTime = b.ModifyTime,
                            InstructionItems = b.InstructionItems
                                .Select(n => new SortingInstructionInfoModel {
                                    Id = n.Id,
                                    CreateTime = n.CreateTime,
                                    ModifyTime = n.ModifyTime,
                                    InstructionBindingId = n.InstructionBindingId,
                                    Remarks = n.Remarks,
                                    Instruction = n.Instruction,
                                    ReplyContent = n.ReplyContent,
                                }).ToList()
                        })
                        .ToListAsync(cancellationToken: token);
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error(e);
                return new List<SortingInstructionBindingInfoModel>();
            }
        }
    }
}