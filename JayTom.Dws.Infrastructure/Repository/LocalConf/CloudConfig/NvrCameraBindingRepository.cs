using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Data.LocalConf.CloudConfig;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf.CloudConfig {

    public class NvrCameraBindingRepository : LocalRepositoryBase<NvrCameraBindingInfoModel>, INvrCameraBindingRepository {

        public NvrCameraBindingRepository(IDbContextFactory<SqliteConfContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}