using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.LocalLog;

namespace JayTom.Dws.Infrastructure.Repository.LocalLog {

    public class VolumeLogRepository : LocalRepositoryBase<VolumeLogInfoModel>, IVolumeLogRepository {

        public VolumeLogRepository(IDbContextFactory<SqliteLogsContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}