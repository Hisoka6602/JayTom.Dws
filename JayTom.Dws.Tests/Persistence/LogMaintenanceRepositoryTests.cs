using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Infrastructure;
using JayTom.Dws.Infrastructure.Repository.LocalLog;
using JayTom.Dws.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JayTom.Dws.Tests.Persistence;

/// <summary>验证日志仓储收敛后的保留期维护与取消契约。</summary>
public sealed class LogMaintenanceRepositoryTests
{
    /// <summary>删除最早日志时只移除最早自然日，保留后续日期。</summary>
    [Fact]
    public async Task DeleteEarliestData_removes_only_earliest_calendar_day()
    {
        string databasePath = CreateDatabasePath();
        try
        {
            DbContextOptions<SqliteLogsContext> options = CreateOptions(databasePath);
            await using (var context = new SqliteLogsContext(options))
            {
                context.Set<AppLogInfoModel>().AddRange(
                    new AppLogInfoModel { CreateTime = new DateTime(2026, 8, 10, 8, 0, 0) },
                    new AppLogInfoModel { CreateTime = new DateTime(2026, 8, 10, 18, 0, 0) },
                    new AppLogInfoModel { CreateTime = new DateTime(2026, 8, 11, 8, 0, 0) });
                await context.SaveChangesAsync();
            }
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var repository = new AppLogRepository(
                new DelegateDbContextFactory<SqliteLogsContext>(() => new SqliteLogsContext(options)),
                cache);

            KeyValuePair<bool, string> result = await repository.DeleteEarliestData();

            Assert.True(result.Key);
            await using var verification = new SqliteLogsContext(options);
            List<DateTime> remaining = await verification.Set<AppLogInfoModel>()
                .AsNoTracking()
                .Select(log => log.CreateTime)
                .ToListAsync();
            Assert.True(remaining.Count == 1);
            Assert.Equal(new DateTime(2026, 8, 11, 8, 0, 0), remaining[0]);
        }
        finally
        {
            DeleteDatabaseArtifacts(databasePath);
        }
    }

    /// <summary>调用方取消必须直接传播，不能被仓储转换为普通失败。</summary>
    [Fact]
    public async Task Maintenance_operations_propagate_cancellation()
    {
        string databasePath = CreateDatabasePath();
        try
        {
            DbContextOptions<SqliteLogsContext> options = CreateOptions(databasePath);
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var repository = new AppLogRepository(
                new DelegateDbContextFactory<SqliteLogsContext>(() => new SqliteLogsContext(options)),
                cache);
            using CancellationTokenSource cancellation = new();
            cancellation.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                repository.DeleteDataThanDays(1, cancellation.Token));
        }
        finally
        {
            DeleteDatabaseArtifacts(databasePath);
        }
    }

    /// <summary>生成当前测试独占的 SQLite 文件路径。</summary>
    private static string CreateDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"jaytom-dws-log-{Guid.NewGuid():N}.db");

    /// <summary>创建不使用连接池的 SQLite 上下文选项。</summary>
    private static DbContextOptions<SqliteLogsContext> CreateOptions(string databasePath) =>
        new DbContextOptionsBuilder<SqliteLogsContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

    /// <summary>删除测试数据库和 SQLite 辅助文件。</summary>
    private static void DeleteDatabaseArtifacts(string databasePath)
    {
        foreach (string path in new[] { databasePath, $"{databasePath}-wal", $"{databasePath}-shm" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
