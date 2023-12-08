using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Data.VideoApiData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Repository.VideoApiData;

namespace JayTom.Dws.Infrastructure.Repository.VideoApiData {

    public class VideoBarCodeRepository : RepositoryBase<VideoBarCodeInfoModel>, IVideoBarCodeRepository {

        public VideoBarCodeRepository(IDbContextFactory<VideoApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}