using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JayTom.Dws.Infrastructure.Repository.LocalLog;

/// <summary>提供清理日志的通用读写与保留期维护。</summary>
public sealed class CleanupLogRepository : LogMaintenanceRepositoryBase<LogCleaningLogInfoModel>, ICleanupLogRepository
{
    /// <summary>创建清理日志仓储。</summary>
    public CleanupLogRepository(IDbContextFactory<SqliteLogsContext> contextFactory, IMemoryCache cache)
        : base(contextFactory, cache) { }
}
