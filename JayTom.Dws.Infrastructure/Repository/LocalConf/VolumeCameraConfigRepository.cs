using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf {

    public class VolumeCameraConfigRepository : LocalRepositoryBase<VolumeCameraConfigInfoModel>, IVolumeCameraConfigRepository {

        public VolumeCameraConfigRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}