using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Models.LocalConf.IpcNvrConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.IpcNvrConfig;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.IpcNvrConfig {

    public class NvrWatermarkConfigRepository : MemoryCacheRepositoryBase<NvrWatermarkConfigInfoModel, SqliteConfContext>, INvrWatermarkConfigRepository {

        public NvrWatermarkConfigRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}