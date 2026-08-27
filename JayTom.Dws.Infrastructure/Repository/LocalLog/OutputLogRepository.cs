using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JayTom.Dws.Infrastructure.Repository.LocalLog;

/// <summary>提供输出日志的通用读写与保留期维护。</summary>
public sealed class OutputLogRepository : LogMaintenanceRepositoryBase<OutputLogInfoModel>, IOutputLogRepository
{
    /// <summary>创建输出日志仓储。</summary>
    public OutputLogRepository(IDbContextFactory<SqliteLogsContext> contextFactory, IMemoryCache cache)
        : base(contextFactory, cache) { }
}
