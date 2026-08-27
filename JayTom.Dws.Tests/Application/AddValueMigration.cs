using JayTom.Dws.Abstractions.Results;
using JayTom.Dws.Application.Configuration;

namespace JayTom.Dws.Tests.Application;

/// <summary>在迁移快照中增加一个可验证的新键。</summary>
internal sealed class AddValueMigration : IConfigurationMigration
{
    /// <summary>源版本。</summary>
    public int FromVersion => 0;

    /// <summary>目标版本。</summary>
    public int ToVersion => 1;

    /// <summary>复制快照并增加测试键。</summary>
    public OperationResult<IReadOnlyDictionary<string, string>> Migrate(
        IReadOnlyDictionary<string, string> source)
    {
        var migrated = new Dictionary<string, string>(source, StringComparer.Ordinal)
        {
            ["added"] = "created"
        };
        return OperationResult<IReadOnlyDictionary<string, string>>.Success(migrated);
    }
}
