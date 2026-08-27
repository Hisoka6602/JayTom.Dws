using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JayTom.Dws.Infrastructure.Repository.LocalLog;

/// <summary>提供分拣日志的通用读写与保留期维护。</summary>
public sealed class SortingLogRepository : LogMaintenanceRepositoryBase<SortingLogInfoModel>, ISortingLogRepository
{
    /// <summary>创建分拣日志仓储。</summary>
    public SortingLogRepository(IDbContextFactory<SqliteLogsContext> contextFactory, IMemoryCache cache)
        : base(contextFactory, cache) { }
}
