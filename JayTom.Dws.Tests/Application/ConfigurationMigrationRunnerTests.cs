using System.Text.Json;
using JayTom.Dws.Abstractions.Results;
using JayTom.Dws.Application.Configuration;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证配置迁移的连续版本、原子提交和精确回滚语义。</summary>
public sealed class ConfigurationMigrationRunnerTests
{
    /// <summary>验证成功迁移只提交一次，并可删除新增键后精确恢复原始快照。</summary>
    [Fact]
    public async Task Migration_receipt_restores_exact_previous_snapshot()
    {
        var store = new InMemorySettingsStore(new Dictionary<string, string>
        {
            ["existing"] = "original"
        });
        var runner = new ConfigurationMigrationRunner(store, new[]
        {
            new AddValueMigration()
        });

        OperationResult<ConfigurationMigrationReceipt> migrated = await runner.MigrateAsync(1);

        Assert.True(migrated.IsSuccess);
        Assert.Equal("created", store.Snapshot["added"]);
        Assert.Equal("1", store.Snapshot[ConfigurationMigrationRunner.SchemaVersionKey]);
        Assert.Equal(1, store.ReplaceCount);

        OperationResult<bool> rolledBack = await runner.RollbackAsync(migrated.Value!);

        Assert.True(rolledBack.IsSuccess);
        Assert.Equal(2, store.ReplaceCount);
        Assert.Equal("original", store.Snapshot["existing"]);
        Assert.DoesNotContain("added", store.Snapshot);
        Assert.DoesNotContain(ConfigurationMigrationRunner.SchemaVersionKey, store.Snapshot);
    }

    /// <summary>验证迁移步骤失败时不会调用持久化替换，原配置保持不变。</summary>
    [Fact]
    public async Task Failed_migration_does_not_commit_partial_snapshot()
    {
        var store = new InMemorySettingsStore(new Dictionary<string, string>
        {
            ["existing"] = "original"
        });
        var runner = new ConfigurationMigrationRunner(store, new[]
        {
            new FailingMigration()
        });

        OperationResult<ConfigurationMigrationReceipt> result = await runner.MigrateAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal("migration.failed", result.ErrorCode);
        Assert.Equal(0, store.ReplaceCount);
        Assert.Equal("original", store.Snapshot["existing"]);
    }

    /// <summary>已经位于目标版本时不得重写数据库或刷新缓存。</summary>
    [Fact]
    public async Task Current_schema_version_is_a_no_write_success()
    {
        var store = new InMemorySettingsStore(new Dictionary<string, string>
        {
            [ConfigurationMigrationRunner.SchemaVersionKey] = "1",
            ["existing"] = "unchanged"
        });
        var runner = new ConfigurationMigrationRunner(store, new[]
        {
            new AddValueMigration()
        });

        OperationResult<ConfigurationMigrationReceipt> result = await runner.MigrateAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value?.FromVersion);
        Assert.Equal(1, result.Value?.ToVersion);
        Assert.Equal(0, store.ReplaceCount);
        Assert.Equal("unchanged", store.Snapshot["existing"]);
    }

}
