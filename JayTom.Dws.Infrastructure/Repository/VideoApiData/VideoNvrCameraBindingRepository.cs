using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.VideoApiData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Data.LocalConf.CloudConfig;
using JayTom.Dws.Domain.Repository.VideoApiData;

namespace JayTom.Dws.Infrastructure.Repository.VideoApiData {

    public class VideoNvrCameraBindingRepository : RepositoryBase<VideoNvrCameraBindingInfoModel, VideoApiContext>, IVideoNvrCameraBindingRepository {

        public VideoNvrCameraBindingRepository(IDbContextFactory<VideoApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}