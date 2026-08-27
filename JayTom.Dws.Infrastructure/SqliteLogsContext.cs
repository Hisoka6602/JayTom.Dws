using JayTom.Dws.Infrastructure.Persistence.ModelConfigurations.Logs;
using Microsoft.EntityFrameworkCore;

namespace JayTom.Dws.Infrastructure;

/// <summary>承载本地日志数据库会话，实体映射由日志模块配置拥有。</summary>
public sealed class SqliteLogsContext : DbContext
{
    /// <summary>创建日志数据库上下文并执行一次性数据库初始化。</summary>
    public SqliteLogsContext(DbContextOptions<SqliteLogsContext> options) : base(options)
    {
        SqliteDatabaseInitializer.EnsureInitialized(
            this,
            SqliteDatabaseInitializer.ResolveDatabasePath(this, "ClientLogs.db"));
    }

    /// <summary>保持既有 SQLite REAL 列结构，同时在业务模型中使用定点数。</summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<decimal>().HaveColumnType("REAL");

    /// <summary>应用日志模块的独立 Fluent Configuration。</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 日志模块配置集中拥有 HasIndex 调用，避免上下文内联实体细节。
        LogModelConfigurations.Apply(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }
}
