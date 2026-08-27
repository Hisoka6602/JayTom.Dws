using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.VideoApiData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Models.LocalConf.CloudConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.VideoApiData;

namespace JayTom.Dws.Infrastructure.Repository.VideoApiData {

    public class VideoNvrCameraBindingRepository : RepositoryBase<VideoNvrCameraBindingInfoModel, VideoApiContext>, IVideoNvrCameraBindingRepository {

        public VideoNvrCameraBindingRepository(IDbContextFactory<VideoApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}