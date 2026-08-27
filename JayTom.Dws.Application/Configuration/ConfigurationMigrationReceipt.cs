namespace JayTom.Dws.Application.Configuration;

/// <summary>记录一次配置迁移前后的版本与可用于精确回滚的原始快照。</summary>
/// <param name="FromVersion">迁移前版本。</param>
/// <param name="ToVersion">迁移后版本。</param>
/// <param name="PreviousSnapshot">迁移前完整快照。</param>
public sealed record ConfigurationMigrationReceipt(
    int FromVersion,
    int ToVersion,
    IReadOnlyDictionary<string, string> PreviousSnapshot);
