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
using JayTom.Dws.Domain.Repository.VideoApi;

namespace JayTom.Dws.Infrastructure.Repository.VideoApi {

    public class VideoConfigRepository : MemoryCacheRepositoryBase<ConfigInfoModel>, IVideoConfigRepository {

        public VideoConfigRepository(IDbContextFactory<VideoApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}