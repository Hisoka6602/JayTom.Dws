using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Application.Configuration;

/// <summary>将既有无版本配置登记为第一版，同时保留全部原始键值。</summary>
public sealed class InitialConfigurationMigration : IConfigurationMigration
{
    /// <summary>接受未登记版本的既有配置。</summary>
    public int FromVersion => 0;

    /// <summary>将既有配置登记为第一版。</summary>
    public int ToVersion => 1;

    /// <summary>复制既有配置，版本键由迁移运行器统一写入。</summary>
    public OperationResult<IReadOnlyDictionary<string, string>> Migrate(
        IReadOnlyDictionary<string, string> source) =>
        OperationResult<IReadOnlyDictionary<string, string>>.Success(
            new Dictionary<string, string>(source, StringComparer.Ordinal));
}
