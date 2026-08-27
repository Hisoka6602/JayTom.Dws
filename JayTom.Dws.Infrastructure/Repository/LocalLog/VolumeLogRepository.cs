using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JayTom.Dws.Infrastructure.Repository.LocalLog;

/// <summary>提供体积日志的通用读写与保留期维护。</summary>
public sealed class VolumeLogRepository : LogMaintenanceRepositoryBase<VolumeLogInfoModel>, IVolumeLogRepository
{
    /// <summary>创建体积日志仓储。</summary>
    public VolumeLogRepository(IDbContextFactory<SqliteLogsContext> contextFactory, IMemoryCache cache)
        : base(contextFactory, cache) { }
}
