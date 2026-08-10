namespace JayTom.Dws.Abstractions.Persistence;

/// <summary>
/// 定义单个应用用例的事务提交边界。
/// </summary>
public interface IUnitOfWork {
    /// <summary>保存当前事务中的全部更改。</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
