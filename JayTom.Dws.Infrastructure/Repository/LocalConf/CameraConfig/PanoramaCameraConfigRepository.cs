using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalConf;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Models.LocalConf.CameraConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.CameraConfig;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.CameraConfig {

    public class PanoramaCameraConfigRepository : MemoryCacheRepositoryBase<PanoramaCameraConfigInfoModel, SqliteConfContext>, IPanoramaCameraConfigRepository {

        public PanoramaCameraConfigRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}