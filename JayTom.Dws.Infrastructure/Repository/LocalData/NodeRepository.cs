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

    public class NodeRepository : LocalRepositoryBase<NodeInfoModel>, INodeRepository {

        public NodeRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public new async Task<bool> Insert([NotNull] NodeInfoModel entity, CancellationToken token = default) {
            var insert = await base.Insert(entity, token);
            if (insert) {
                //加入缓存
                base._cache.Set($"{entity.PackageId}-{entity.NodeNum}", entity, new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5)));
            }
            return insert;
        }

        public async Task<NodeInfoModel?> GetMemoryCacheNodeInfo(long packageId, int nodeIndex, CancellationToken token = default) {
            return await _cache.GetOrCreateAsync($"{packageId}-{nodeIndex}", async cacheEntry => {
                // 设置缓存项的过期时间为2分钟
                cacheEntry.SetSlidingExpiration(TimeSpan.FromMinutes(5));
                // 如果缓存中没有该键，则调用 FirstOrDefaultInfo 方法从数据库获取数据
                var result = await FirstOrDefault(x => x.PackageId == packageId &&
                    x.NodeNum == nodeIndex, token);

                return result;
            });
        }
    }
}