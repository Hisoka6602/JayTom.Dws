using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JayTom.Dws.Infrastructure.Repository.LocalLog;

/// <summary>提供应用日志的通用读写与保留期维护。</summary>
public sealed class AppLogRepository : LogMaintenanceRepositoryBase<AppLogInfoModel>, IAppLogRepository
{
    /// <summary>创建应用日志仓储。</summary>
    public AppLogRepository(IDbContextFactory<SqliteLogsContext> contextFactory, IMemoryCache cache)
        : base(contextFactory, cache) { }
}
