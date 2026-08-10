using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.ConnectionParams;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig.ConnectionParams {

    public class TcpConnectionConfigRepository : LocalRepositoryBase<TcpConnectionConfigInfoModel, SqliteConfContext>, ITcpConnectionConfigRepository {

        public TcpConnectionConfigRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}