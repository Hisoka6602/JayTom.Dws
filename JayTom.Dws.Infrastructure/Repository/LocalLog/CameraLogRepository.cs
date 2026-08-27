using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JayTom.Dws.Infrastructure.Repository.LocalLog;

/// <summary>提供相机日志的通用读写与保留期维护。</summary>
public sealed class CameraLogRepository : LogMaintenanceRepositoryBase<CameraLogInfoModel>, ICameraLogRepository
{
    /// <summary>创建相机日志仓储。</summary>
    public CameraLogRepository(IDbContextFactory<SqliteLogsContext> contextFactory, IMemoryCache cache)
        : base(contextFactory, cache) { }
}
