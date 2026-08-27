using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JayTom.Dws.Infrastructure.Repository.LocalLog;

/// <summary>提供 API 日志的通用读写与保留期维护。</summary>
public sealed class ApiLogRepository : LogMaintenanceRepositoryBase<ApiLogInfoModel>, IApiLogRepository
{
    /// <summary>创建 API 日志仓储。</summary>
    public ApiLogRepository(IDbContextFactory<SqliteLogsContext> contextFactory, IMemoryCache cache)
        : base(contextFactory, cache) { }
}
