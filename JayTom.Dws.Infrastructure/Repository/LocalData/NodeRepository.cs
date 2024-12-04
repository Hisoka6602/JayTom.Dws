using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.LocalData;

namespace JayTom.Dws.Infrastructure.Repository.LocalData {

    public class NodeRepository : LocalRepositoryBase<NodeInfoModel>, INodeRepository {

        public NodeRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}