namespace JayTom.Dws.Legacy.Contracts.Repositories;

/// <summary>
/// 组合实体查询与写入能力的兼容仓储边界。
/// </summary>
/// <typeparam name="T">实体类型。</typeparam>
public interface IRepository<T> : IReadRepository<T>, IWriteRepository<T>
    where T : class;
