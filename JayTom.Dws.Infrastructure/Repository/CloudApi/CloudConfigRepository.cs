using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Data.CloudApiData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.CloudApi;

namespace JayTom.Dws.Infrastructure.Repository.CloudApi {

    public class CloudConfigRepository : MemoryCacheRepositoryBase<ConfigInfoModel>, ICloudConfigRepository {

        public CloudConfigRepository(IDbContextFactory<CloudApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}