using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.LocalData;

namespace JayTom.Dws.Infrastructure.Repository.LocalData {

    public class SortingRepository : LocalRepositoryBase<SortingInfoModel>, ISortingRepository {

        public SortingRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(
            contextFactory, cache) {
        }

        public new async Task<bool> Insert([NotNull] SortingInfoModel entity, CancellationToken token) {
            var insert = await base.Insert(entity, token);
            if (insert) {
                var infoModel = base._cache.Get<PackageInfoModel>(entity.PackageId);
                if (infoModel is not null) {
                    infoModel.SortingInfo = entity;
                }
            }
            return insert;
        }

        public new async Task<SortingInfoModel?> FirstOrDefault([NotNull] Expression<Func<SortingInfoModel, bool>> @where,
            CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<SortingInfoModel>();
                if (dbSet is null) return null;
                var sortingInfoModel = await dbSet.AsNoTracking()
                    .Where(where)
                    .Include(b => b.InstructionInfos)
                    .FirstOrDefaultAsync(cancellationToken: token);
                return sortingInfoModel;
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return null;
            }
        }
    }
}