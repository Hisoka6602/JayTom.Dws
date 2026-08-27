using JayTom.Dws.Abstractions.Results;
using JayTom.Dws.Application.Configuration;

namespace JayTom.Dws.Tests.Application;

/// <summary>始终返回稳定错误的失败迁移。</summary>
internal sealed class FailingMigration : IConfigurationMigration
{
    /// <summary>源版本。</summary>
    public int FromVersion => 0;

    /// <summary>目标版本。</summary>
    public int ToVersion => 1;

    /// <summary>返回失败而不修改输入快照。</summary>
    public OperationResult<IReadOnlyDictionary<string, string>> Migrate(
        IReadOnlyDictionary<string, string> source) =>
        OperationResult<IReadOnlyDictionary<string, string>>.Failure(
            "migration.failed",
            "测试迁移失败。");
}
