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

    public class FtpLogRepository : LocalRepositoryBase<FtpLogInfoModel>, IFtpLogRepository {

        public FtpLogRepository(IDbContextFactory<SqliteLogsContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }
    }
}