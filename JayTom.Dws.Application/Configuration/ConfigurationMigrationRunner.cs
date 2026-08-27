using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Application.Configuration;

/// <summary>按连续版本在内存中执行迁移，并通过原子快照替换完成提交或回滚。</summary>
public sealed class ConfigurationMigrationRunner
{
    /// <summary>当前应用支持的配置架构版本。</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>持久化配置架构版本的保留键。</summary>
    public const string SchemaVersionKey = "__DwsConfigurationSchemaVersion";
    /// <summary>配置快照持久化边界。</summary>
    private readonly ISettingsStore _settingsStore;
    /// <summary>按源版本索引的连续迁移步骤。</summary>
    private readonly IReadOnlyDictionary<int, IConfigurationMigration> _migrations;

    /// <summary>创建迁移运行器并拒绝重复源版本。</summary>
    public ConfigurationMigrationRunner(
        ISettingsStore settingsStore,
        IEnumerable<IConfigurationMigration> migrations)
    {
        _settingsStore = settingsStore;
        _migrations = migrations.ToDictionary(migration => migration.FromVersion);
    }

    /// <summary>迁移到指定版本；全部步骤成功后才原子提交完整快照。</summary>
    public async Task<OperationResult<ConfigurationMigrationReceipt>> MigrateAsync(
        int targetVersion,
        CancellationToken cancellationToken = default)
    {
        if (targetVersion < 0)
        {
            return OperationResult<ConfigurationMigrationReceipt>.Failure(
                "configuration.invalid_target_version",
                "目标配置版本不能为负数。");
        }

        IReadOnlyDictionary<string, string> original = await _settingsStore
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);
        int currentVersion = ParseVersion(original);
        if (currentVersion > targetVersion)
        {
            return OperationResult<ConfigurationMigrationReceipt>.Failure(
                "configuration.downgrade_requires_rollback",
                "配置降级必须使用已保存的迁移回执执行精确回滚。");
        }
        if (currentVersion == targetVersion)
        {
            return OperationResult<ConfigurationMigrationReceipt>.Success(
                new ConfigurationMigrationReceipt(
                    currentVersion,
                    currentVersion,
                    new Dictionary<string, string>(original, StringComparer.Ordinal)));
        }

        var working = new Dictionary<string, string>(original, StringComparer.Ordinal);
        int fromVersion = currentVersion;
        while (currentVersion < targetVersion)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_migrations.TryGetValue(currentVersion, out var migration) ||
                migration.ToVersion != currentVersion + 1)
            {
                return OperationResult<ConfigurationMigrationReceipt>.Failure(
                    "configuration.migration_gap",
                    $"缺少从配置版本 {currentVersion} 到 {currentVersion + 1} 的连续迁移。");
            }

            OperationResult<IReadOnlyDictionary<string, string>> migrated = migration.Migrate(working);
            if (!migrated.IsSuccess || migrated.Value is null)
            {
                return OperationResult<ConfigurationMigrationReceipt>.Failure(
                    migrated.ErrorCode,
                    migrated.ErrorMessage);
            }
            working = new Dictionary<string, string>(migrated.Value, StringComparer.Ordinal);
            currentVersion = migration.ToVersion;
        }

        working[SchemaVersionKey] = currentVersion.ToString(System.Globalization.CultureInfo.InvariantCulture);
        bool saved = await _settingsStore
            .ReplaceSnapshotAsync(working, cancellationToken)
            .ConfigureAwait(false);
        return saved
            ? OperationResult<ConfigurationMigrationReceipt>.Success(new ConfigurationMigrationReceipt(
                fromVersion,
                currentVersion,
                new Dictionary<string, string>(original, StringComparer.Ordinal)))
            : OperationResult<ConfigurationMigrationReceipt>.Failure(
                "configuration.commit_failed",
                "配置迁移快照未能提交。");
    }

    /// <summary>使用迁移回执在单一事务内精确恢复迁移前快照。</summary>
    public async Task<OperationResult<bool>> RollbackAsync(
        ConfigurationMigrationReceipt receipt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        bool restored = await _settingsStore
            .ReplaceSnapshotAsync(receipt.PreviousSnapshot, cancellationToken)
            .ConfigureAwait(false);
        return restored
            ? OperationResult<bool>.Success(true)
            : OperationResult<bool>.Failure(
                "configuration.rollback_failed",
                "配置迁移快照未能回滚。");
    }

    /// <summary>从快照读取非负架构版本，缺失时按零版本处理。</summary>
    private static int ParseVersion(IReadOnlyDictionary<string, string> snapshot)
    {
        if (!snapshot.TryGetValue(SchemaVersionKey, out string? value))
        {
            return 0;
        }
        if (!int.TryParse(value, out int version) || version < 0)
        {
            throw new InvalidDataException("配置架构版本不是有效的非负整数。");
        }
        return version;
    }
}
