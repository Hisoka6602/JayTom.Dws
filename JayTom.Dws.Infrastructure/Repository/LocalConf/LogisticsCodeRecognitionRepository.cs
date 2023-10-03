using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Infrastructure.Repository.LocalConf {

    public class LogisticsCodeRecognitionRepository : LocalRepositoryBase<LogisticsCodeRecognitionInfoModel>, ILogisticsCodeRecognitionRepository {

        public LogisticsCodeRecognitionRepository(IDbContextFactory<SqliteContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}