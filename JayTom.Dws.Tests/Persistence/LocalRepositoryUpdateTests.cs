using System.Data.Common;
using JayTom.Dws.Infrastructure;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.Models.Package;
using JayTom.Dws.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;

namespace JayTom.Dws.Tests.Persistence;

/// <summary>验证密集更新省去外层事务后，单命令开销和多命令回滚仍满足生产约束。</summary>
public sealed class LocalRepositoryUpdateTests
{
    /// <summary>格口单条更新由数据库语句原子执行，不额外打开用户事务与保存点。</summary>
    [Fact]
    public async Task Single_exit_update_does_not_start_an_extra_transaction()
    {
        await WithDatabase(async (options, transactions) =>
        {
            var package = new PackageInfoModel { PackageTimestamped = 1,
                PackageCreateTime = DateTime.Now, ExitInfo = new() { TheoreticalExit = "OLD" } };
            await using (var db = new SqliteContext(options))
            {
                db.Add(package);
                await db.SaveChangesAsync();
            }
            transactions.Started = 0;
            package.ExitInfo!.TheoreticalExit = "NEW";
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var repository = new ExitInfoRepository(new DelegateDbContextFactory<SqliteContext>(() => new(options)), cache);
            Assert.True(await repository.Update(package.ExitInfo, CancellationToken.None));
            Assert.Equal(0, transactions.Started);
            await using var verify = new SqliteContext(options);
            Assert.Equal("NEW", (await verify.Set<ExitInfoModel>().AsNoTracking().FirstAsync()).TheoreticalExit);
        });
    }

    /// <summary>实体图后续指令写入失败时，已执行的分拣更新和其他指令不能部分提交。</summary>
    [Fact]
    public async Task Failing_instruction_rolls_back_the_entire_sorting_update_and_can_recover()
    {
        await WithDatabase(async (options, transactions) =>
        {
            var package = new PackageInfoModel { PackageTimestamped = 2,
                PackageCreateTime = DateTime.Now, SortingInfo = new() { SortingCode = "OLD",
                    InstructionInfos = [new() { InstructionContent = "ORIGINAL" }] } };
            await using (var db = new SqliteContext(options))
            {
                db.Add(package);
                await db.SaveChangesAsync();
                await ExecuteFixtureCommand(db, """
                    CREATE TRIGGER reject_test_instruction BEFORE INSERT ON Data_InstructionInfo
                    WHEN NEW.InstructionContent = 'FAIL' BEGIN SELECT RAISE(ABORT, 'test failure'); END;
                    """);
            }
            transactions.Started = 0;
            var sorting = package.SortingInfo!;
            sorting.SortingCode = "NEW";
            sorting.InstructionInfos!.Add(new() { InstructionContent = "GOOD" });
            sorting.InstructionInfos.Add(new() { InstructionContent = "FAIL" });
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var repository = new SortingRepository(new DelegateDbContextFactory<SqliteContext>(() => new(options)), cache);
            Assert.False(await repository.Update(sorting, CancellationToken.None));
            Assert.True(transactions.Started > 0);
            await using (var verify = new SqliteContext(options))
            {
                var stored = await verify.Set<SortingInfoModel>().AsNoTracking().Include(row => row.InstructionInfos).FirstAsync();
                Assert.Equal("OLD", stored.SortingCode);
                Assert.Collection(stored.InstructionInfos!, row => Assert.Equal("ORIGINAL", row.InstructionContent));
                await ExecuteFixtureCommand(verify, "DROP TRIGGER reject_test_instruction");
            }
            // 从已回滚的数据库重新获取身份，验证下一次真实工作可以提交。
            await using var reload = new SqliteContext(options);
            var recovered = await reload.Set<SortingInfoModel>().AsNoTracking().Include(row => row.InstructionInfos).FirstAsync();
            recovered.SortingCode = "RECOVERED";
            recovered.InstructionInfos!.Add(new() { InstructionContent = "AFTER-FAILURE" });
            Assert.True(await repository.Update(recovered, CancellationToken.None));
            await using var final = new SqliteContext(options);
            Assert.Equal("RECOVERED", (await final.Set<SortingInfoModel>().AsNoTracking().FirstAsync()).SortingCode);
            Assert.Equal(2, await final.Set<InstructionInfoModel>().CountAsync());
        });
    }

    /// <summary>通过测试数据库连接安装或清除故障注入触发器，不进入生产仓储查询。</summary>
    private static async Task ExecuteFixtureCommand(SqliteContext context, string schemaCommand)
    {
        await context.Database.OpenConnectionAsync();
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = schemaCommand;
        await command.ExecuteNonQueryAsync();
        await context.Database.CloseConnectionAsync();
    }

    /// <summary>为每次测试建立独立真实 SQLite 文件，不接触设备或生产历史。</summary>
    private static async Task WithDatabase(Func<DbContextOptions<SqliteContext>, TransactionCounter, Task> test)
    {
        var path = Path.Combine(Path.GetTempPath(), $"jaytom-update-{Guid.NewGuid():N}.db");
        var counter = new TransactionCounter();
        var options = new DbContextOptionsBuilder<SqliteContext>().UseSqlite($"Data Source={path};Pooling=False")
            .AddInterceptors(counter).Options;
        try { await test(options, counter); }
        finally
        {
            foreach (var file in new[] { path, path + "-wal", path + "-shm" })
                if (File.Exists(file)) File.Delete(file);
        }
    }

    /// <summary>计数实际事务开始，覆盖默认事务策略而非仅检查源代码。</summary>
    private sealed class TransactionCounter : DbTransactionInterceptor
    {
        /// <summary>当前测试观测到的事务次数。</summary>
        internal int Started;

        /// <summary>记录同步事务路径。</summary>
        public override InterceptionResult<DbTransaction> TransactionStarting(DbConnection connection,
            TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result)
        {
            Started++;
            return result;
        }

        /// <summary>记录异步事务路径。</summary>
        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection,
            TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default)
        {
            Started++;
            return ValueTask.FromResult(result);
        }
    }
}
