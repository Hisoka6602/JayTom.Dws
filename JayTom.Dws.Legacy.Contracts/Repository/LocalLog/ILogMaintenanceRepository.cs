using JayTom.Dws.Models.LocalLog;

namespace JayTom.Dws.Legacy.Contracts.Repositories.LocalLog;

/// <summary>聚合日志读写与保留期维护能力，供日志用例依赖单一端口。</summary>
/// <typeparam name="TLog">日志实体类型。</typeparam>
public interface ILogMaintenanceRepository<TLog> : IRepository<TLog>
    where TLog : BaseLogInfoModel
{
    /// <summary>删除指定保留天数之前的日志。</summary>
    Task<KeyValuePair<bool, string>> DeleteDataThanDays(
        int days,
        CancellationToken cancellationToken = default);

    /// <summary>删除数据库中最早自然日的日志。</summary>
    Task<KeyValuePair<bool, string>> DeleteEarliestData(
        CancellationToken cancellationToken = default);
}
