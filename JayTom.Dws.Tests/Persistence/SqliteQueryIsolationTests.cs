using JayTom.Dws.Application.Deployment;
using JayTom.Dws.Application.PackageHistory;
using JayTom.Dws.Infrastructure;
using JayTom.Dws.Infrastructure.Configuration;
using JayTom.Dws.Infrastructure.DependencyInjection;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.Models.Package;
using JayTom.Dws.Tests.TestDoubles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace JayTom.Dws.Tests.Persistence;

/// <summary>验证真实 SQLite 历史读取不锁住包裹写入，并能中断和恢复查询。</summary>
public sealed class SqliteQueryIsolationTests
{
    /// <summary>列表省略大报文但保留分拣指令，打开单包裹详情时仍能完整读回报文。</summary>
    [Fact]
    public async Task List_reads_small_upload_fields_and_details_restore_full_payload()
    {
        await WithDatabase(async options =>
        {
            await using var db = new SqliteContext(options);
            var package = Package("NoRead", 1);
            var body = new string('R', 512000);
            package.UploadInfo = new() { RequestStatus = UploadStatus.Succeeded, RequestContent = body, ResponseContent = body };
            package.SortingInfo = new() { SortingCode = "17", InstructionInfos = [new() { InstructionContent = "F9 11", InstructionType = InstructionType.SendSorting }] };
            db.Add(package);
            await db.SaveChangesAsync();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new PackageHistoryQueryService(new PackageRepository(new DelegateDbContextFactory<SqliteContext>(() => new(options)), cache));
            var page = await service.SearchAsync(new(), 0, 100);
            Assert.True(page.Items.Count == 1);
            var item = page.Items[0];
            Assert.Equal(UploadStatus.Succeeded, item.UploadInfo?.RequestStatus);
            Assert.Equal(string.Empty, item.UploadInfo?.RequestContent);
            Assert.Equal("17", item.SortingInfo?.SortingCode);
            Assert.Equal(1, item.SortingInfo?.InstructionInfos.Count);
            var detail = await service.SearchAsync(new(PackageTimestamp: 1), 0, 1);
            Assert.Equal(body, detail.Items[0].UploadInfo?.RequestContent);
            Assert.Equal(body, detail.Items[0].UploadInfo?.ResponseContent);
        });
    }

    /// <summary>原生进度回调可以取消没有返回任何行的长统计，并不会污染后续写入。</summary>
    [Fact]
    public async Task Native_long_query_is_cancelled_and_connection_remains_writable()
    {
        await WithDatabase(async options =>
        {
            await using var db = new SqliteContext(options);
            using var cancellation = new CancellationTokenSource();
            using (var scope = new SqliteReadCancellationScope(db, cancellation.Token))
            using (var command = db.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "WITH RECURSIVE n(x) AS (VALUES(1) UNION ALL SELECT x+1 FROM n WHERE x<10000000) SELECT sum(x) FROM n";
                cancellation.CancelAfter(TimeSpan.FromMilliseconds(20));
                var error = Assert.Throws<SqliteException>(() => command.ExecuteScalar());
                Assert.Equal(9, error.SqliteErrorCode);
            }
            db.Add(Package("AFTER-CANCEL", 1));
            Assert.True(await db.SaveChangesAsync() > 0);
            Assert.Equal(1, await db.Set<PackageInfoModel>().CountAsync());
        });
    }

    /// <summary>取消的统计向上抛出取消结果，不伪装为没有历史记录。</summary>
    [Fact]
    public async Task Cancelled_history_count_is_not_reported_as_zero()
    {
        await WithDatabase(async options =>
        {
            await using var db = new SqliteContext(options);
            db.Add(Package("NoRead", 1));
            await db.SaveChangesAsync();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var repository = new PackageRepository(new DelegateDbContextFactory<SqliteContext>(() => new(options)), cache);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.Total(item => true, cancellation.Token));
            var service = new PackageHistoryQueryService(repository);
            var page = await service.SearchAsync(new(), 0, 100);
            Assert.Equal(1, page.Total);
            Assert.True(page.Items.Count == 1);
            Assert.Equal("NoRead", page.Items[0].BarCodeInfo?.Barcode);
        });
    }

    /// <summary>数据库故障作为失败返回查询界面，恢复后查询线程继续工作。</summary>
    [Fact]
    public async Task Database_failure_propagates_and_a_later_query_succeeds()
    {
        await WithDatabase(async options =>
        {
            await using var db = new SqliteContext(options);
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var repository = new PackageRepository(new DelegateDbContextFactory<SqliteContext>(() => new(options)), cache);
            var service = new PackageHistoryQueryService(repository);
            await db.Database.OpenConnectionAsync();
            using var schema = db.Database.GetDbConnection().CreateCommand();
            schema.CommandText = "ALTER TABLE Data_BarCodeInfo RENAME TO Saved_BarCodeInfo";
            schema.ExecuteNonQuery();
            await Assert.ThrowsAsync<SqliteException>(() => service.SearchAsync(new(), 0, 100));
            schema.CommandText = "ALTER TABLE Saved_BarCodeInfo RENAME TO Data_BarCodeInfo";
            schema.ExecuteNonQuery();
            Assert.Equal(0, (await service.SearchAsync(new(), 0, 100)).Total);
        });
    }

    /// <summary>生产连接配置下，固定历史读取快照期间另一个连接仍然能提交包裹。</summary>
    [Fact]
    public async Task Production_WAL_reader_does_not_block_sorting_writes()
    {
        var root = Path.Combine(Path.GetTempPath(), "dws-query-wal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var paths = new DefaultApplicationPathProvider(new ApplicationPathOptions {
            DatabaseDirectory = root, DataDirectory = root, ConfigurationDirectory = root,
            LogDirectory = root, ModelDirectory = root, AdapterPackDirectory = root
        });
        var services = new ServiceCollection();
        services.AddSingleton<IApplicationPathProvider>(paths);
        services.AddDwsPersistence();
        string? connectionString = null;
        try
        {
            await using var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IDbContextFactory<SqliteContext>>();
            await using var reader = await factory.CreateDbContextAsync();
            connectionString = reader.Database.GetDbConnection().ConnectionString;
            Assert.Equal(SqliteCacheMode.Private, new SqliteConnectionStringBuilder(connectionString).Cache);
            reader.Add(Package("FIRST", 1));
            await reader.SaveChangesAsync();
            await reader.Database.OpenConnectionAsync();
            using var nativeTransaction = ((SqliteConnection)reader.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var transaction = await reader.Database.UseTransactionAsync(nativeTransaction);
            Assert.Equal(1, await reader.Set<PackageInfoModel>().CountAsync());
            var write = Task.Run(async () =>
            {
                await using var writer = await factory.CreateDbContextAsync();
                writer.Add(Package("SECOND", 2));
                return await writer.SaveChangesAsync();
            });
            Assert.True(await write.WaitAsync(TimeSpan.FromSeconds(3)) > 0);
            Assert.Equal(1, await reader.Set<PackageInfoModel>().CountAsync());
            await transaction!.CommitAsync();
            Assert.Equal(2, await reader.Set<PackageInfoModel>().CountAsync());
        }
        finally
        {
            if (connectionString is not null) { using var connection = new SqliteConnection(connectionString); SqliteConnection.ClearPool(connection); }
            foreach (var file in Directory.GetFiles(root)) File.Delete(file);
            if (!Directory.EnumerateFileSystemEntries(root).Any()) Directory.Delete(root);
        }
    }

    /// <summary>创建可读取的测试包裹，包含 NoRead 所需的原始条码结构。</summary>
    private static PackageInfoModel Package(string barcode, long timestamp) => new()
    {
        PackageTimestamped = timestamp, PackageCreateTime = new DateTime(2026, 10, 4).AddSeconds(timestamp),
        BarCodeInfo = new() { Barcode = barcode, ScanTime = new DateTime(2026, 10, 4).AddSeconds(timestamp) }
    };

    /// <summary>每个用例独占文件数据库并关闭连接后清理，避免测试之间共享查询和写锁。</summary>
    private static async Task WithDatabase(Func<DbContextOptions<SqliteContext>, Task> test)
    {
        var path = Path.Combine(Path.GetTempPath(), "dws-query-" + Guid.NewGuid().ToString("N") + ".db");
        var options = new DbContextOptionsBuilder<SqliteContext>().UseSqlite("Data Source=" + path + ";Pooling=False").Options;
        try { await test(options); }
        finally { foreach (var suffix in new[] { "", "-wal", "-shm" }) if (File.Exists(path + suffix)) File.Delete(path + suffix); }
    }
}
