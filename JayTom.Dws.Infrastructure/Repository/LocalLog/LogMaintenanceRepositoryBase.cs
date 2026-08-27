using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JayTom.Dws.Infrastructure.Repository.LocalLog;

/// <summary>集中实现日志保留期清理，避免每种日志复制数据库读写流程。</summary>
/// <typeparam name="TLog">日志实体类型。</typeparam>
public abstract class LogMaintenanceRepositoryBase<TLog> :
    LocalRepositoryBase<TLog, SqliteLogsContext>,
    ILogMaintenanceRepository<TLog>
    where TLog : BaseLogInfoModel
{
    /// <summary>创建日志维护仓储。</summary>
    protected LogMaintenanceRepositoryBase(
        IDbContextFactory<SqliteLogsContext> contextFactory,
        IMemoryCache cache)
        : base(contextFactory, cache)
    {
    }

    /// <summary>删除指定保留天数之前的日志，并把取消令牌传递到数据库。</summary>
    public async Task<KeyValuePair<bool, string>> DeleteDataThanDays(
        int days,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(days);
        try
        {
            await using SqliteLogsContext context =
                await _contextFactory.CreateDbContextAsync(cancellationToken);
            DateTime cutoff = TimeProvider.System.GetLocalNow().DateTime.AddDays(-days);
            await context.Set<TLog>()
                .Where(log => log.CreateTime <= cutoff)
                .ExecuteDeleteAsync(cancellationToken);
            return new KeyValuePair<bool, string>(true, "删除成功");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            NLog.LogManager.GetCurrentClassLogger().Error(exception);
            return new KeyValuePair<bool, string>(false, "删除失败");
        }
    }

    /// <summary>只投影时间列定位最早自然日，再批量删除该日日志。</summary>
    public async Task<KeyValuePair<bool, string>> DeleteEarliestData(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using SqliteLogsContext context =
                await _contextFactory.CreateDbContextAsync(cancellationToken);
            DateTime? earliest = await context.Set<TLog>()
                .AsNoTracking()
                .OrderBy(log => log.CreateTime)
                .Select(log => (DateTime?)log.CreateTime)
                .FirstOrDefaultAsync(cancellationToken);
            if (earliest is null)
            {
                return new KeyValuePair<bool, string>(true, "没有需要删除的数据");
            }

            DateTime nextDay = earliest.Value.Date.AddDays(1);
            await context.Set<TLog>()
                .Where(log => log.CreateTime < nextDay)
                .ExecuteDeleteAsync(cancellationToken);
            return new KeyValuePair<bool, string>(true, "删除成功");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            NLog.LogManager.GetCurrentClassLogger().Error(exception);
            return new KeyValuePair<bool, string>(false, "删除失败");
        }
    }
}
