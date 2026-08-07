using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
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

        public new async Task<bool> Insert(SortingInfoModel entity, CancellationToken token) {
            var insert = await base.Insert(entity, token);
            if (insert) {
                var infoModel = base._cache.Get<PackageInfoModel>(entity.PackageId);
                infoModel?.SortingInfo = entity;
            }
            return insert;
        }
    }
}
