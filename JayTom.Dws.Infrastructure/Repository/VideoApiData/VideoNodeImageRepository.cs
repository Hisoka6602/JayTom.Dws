using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Models.VideoApiData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalData;
using JayTom.Dws.Legacy.Contracts.Repositories.VideoApiData;

namespace JayTom.Dws.Infrastructure.Repository.VideoApiData {

    public class VideoNodeImageRepository : RepositoryBase<VideoNodeImageInfoModel, VideoApiContext>, IVideoNodeImageRepository {

        public VideoNodeImageRepository(IDbContextFactory<VideoApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}