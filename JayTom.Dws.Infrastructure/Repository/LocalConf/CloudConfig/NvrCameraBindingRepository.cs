using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalConf;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Models.LocalConf.CloudConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.CloudConfig;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.CloudConfig {

    public class NvrCameraBindingRepository : MemoryCacheRepositoryBase<NvrCameraBindingInfoModel, SqliteConfContext>, INvrCameraBindingRepository {

        public NvrCameraBindingRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}