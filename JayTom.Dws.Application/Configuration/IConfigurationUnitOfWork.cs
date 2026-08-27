namespace JayTom.Dws.Application.Configuration;

/// <summary>定义配置聚合在单一事务中替换完整快照的工作单元。</summary>
public interface IConfigurationUnitOfWork
{
    /// <summary>原子替换全部配置并在提交后同步读取快照。</summary>
    Task<bool> ReplaceSnapshotAsync(
        IReadOnlyDictionary<string, string> snapshot,
        CancellationToken cancellationToken = default);
}
