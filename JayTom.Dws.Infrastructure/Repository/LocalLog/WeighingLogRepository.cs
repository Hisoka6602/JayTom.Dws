using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JayTom.Dws.Infrastructure.Repository.LocalLog;

/// <summary>提供称重日志的通用读写与保留期维护。</summary>
public sealed class WeighingLogRepository : LogMaintenanceRepositoryBase<WeighingLogInfoModel>, IWeighingLogRepository
{
    /// <summary>创建称重日志仓储。</summary>
    public WeighingLogRepository(IDbContextFactory<SqliteLogsContext> contextFactory, IMemoryCache cache)
        : base(contextFactory, cache) { }
}
