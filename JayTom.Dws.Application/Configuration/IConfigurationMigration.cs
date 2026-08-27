using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Application.Configuration;

/// <summary>定义一个纯函数式、可按版本排序的应用配置迁移步骤。</summary>
public interface IConfigurationMigration
{
    /// <summary>迁移接受的源版本。</summary>
    int FromVersion { get; }

    /// <summary>迁移完成后的目标版本。</summary>
    int ToVersion { get; }

    /// <summary>在内存快照上执行迁移，失败时不得修改持久化配置。</summary>
    OperationResult<IReadOnlyDictionary<string, string>> Migrate(
        IReadOnlyDictionary<string, string> source);
}
