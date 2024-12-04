using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.CameraConfig {

    public class ScanNodeConfigRepository : MemoryCacheRepositoryBase<ScanNodeConfigInfoModel>, IScanNodeConfigRepository {

        public ScanNodeConfigRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}