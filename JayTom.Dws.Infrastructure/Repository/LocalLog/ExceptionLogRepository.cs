using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JayTom.Dws.Infrastructure.Repository.LocalLog;

/// <summary>提供异常日志的通用读写与保留期维护。</summary>
public sealed class ExceptionLogRepository : LogMaintenanceRepositoryBase<ExceptionLogInfoModel>, IExceptionLogRepository
{
    /// <summary>创建异常日志仓储。</summary>
    public ExceptionLogRepository(IDbContextFactory<SqliteLogsContext> contextFactory, IMemoryCache cache)
        : base(contextFactory, cache) { }
}
