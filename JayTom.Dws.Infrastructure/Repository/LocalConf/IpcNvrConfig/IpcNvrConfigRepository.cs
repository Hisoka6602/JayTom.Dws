using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Data.LocalConf.IpcNvrConfig;
using JayTom.Dws.Domain.Repository.LocalConf.IpcNvrConfig;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.IpcNvrConfig {

    public class IpcNvrConfigRepository : MemoryCacheRepositoryBase<IpcNvrConfigInfoModel>, IIpcNvrConfigRepository {

        public IpcNvrConfigRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}