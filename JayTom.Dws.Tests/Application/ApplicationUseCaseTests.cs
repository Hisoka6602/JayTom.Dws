using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Application.PackageHistory;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证应用 Command、Query、Validator 和 DTO 映射边界。</summary>
public sealed class ApplicationUseCaseTests
{
    /// <summary>非法命令必须在进入事务存储前返回稳定校验错误。</summary>
    [Fact]
    public async Task Configuration_command_rejects_invalid_input_before_persistence()
    {
        var store = new InMemorySettingsStore(new Dictionary<string, string>());
        var handler = new MigrateConfigurationCommandHandler(
            new ConfigurationMigrationRunner(store, [new AddValueMigration()]),
            new MigrateConfigurationCommandValidator());

        var result = await handler.HandleAsync(new MigrateConfigurationCommand(-1));

        Assert.False(result.IsSuccess);
        Assert.Equal("configuration.invalid_target_version", result.ErrorCode);
        Assert.Equal(0, store.ReplaceCount);
    }

    /// <summary>合法事务命令通过连续迁移并只提交一次完整快照。</summary>
    [Fact]
    public async Task Configuration_command_commits_one_atomic_snapshot()
    {
        var store = new InMemorySettingsStore(new Dictionary<string, string>());
        var handler = new MigrateConfigurationCommandHandler(
            new ConfigurationMigrationRunner(store, [new AddValueMigration()]),
            new MigrateConfigurationCommandValidator());

        var result = await handler.HandleAsync(new MigrateConfigurationCommand(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, store.ReplaceCount);
        Assert.Equal("created", store.Snapshot["added"]);
    }

    /// <summary>Query Handler 将强类型筛选和分页参数完整传递给读取边界。</summary>
    [Fact]
    public async Task Package_history_query_handler_forwards_typed_query()
    {
        var service = new StubPackageHistoryQueryService();
        var handler = new SearchPackageHistoryQueryHandler(
            service,
            new SearchPackageHistoryQueryValidator());
        var filter = new PackageHistoryQuery(Barcode: "JT-QUERY");

        PackageHistoryPage result = await handler.HandleAsync(
            new SearchPackageHistoryQuery(filter, 2, 50));

        Assert.Same(service.Result, result);
        Assert.Same(filter, service.LastFilter);
        Assert.Equal(2, service.LastPageIndex);
        Assert.Equal(50, service.LastPageSize);
    }

    /// <summary>查询校验器集中拒绝反向时间、重量范围和过大分页。</summary>
    [Fact]
    public void Package_history_validator_returns_stable_errors()
    {
        var validator = new SearchPackageHistoryQueryValidator();
        var request = new SearchPackageHistoryQuery(
            new PackageHistoryQuery(
                StartTime: new DateTime(2026, 8, 15),
                EndTime: new DateTime(2026, 8, 14),
                MinWeight: 10,
                MaxWeight: 1),
            0,
            1001);

        var errors = validator.Validate(request);

        Assert.Equal(
            [
                "package_history.invalid_page_size",
                "package_history.invalid_time_range",
                "package_history.invalid_weight_range"
            ],
            errors.Select(error => error.Code));
    }
}
