using System.Data.Common;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace JayTom.Dws.Infrastructure;

/// <summary>
/// 为连接池中的每个 SQLite 连接仅配置一次连接级性能参数，避免只初始化首个连接或每次查询重复执行。
/// </summary>
internal sealed class SqliteConnectionPragmaInterceptor : DbConnectionInterceptor
{
    /// <summary>保存供所有 SQLite 上下文复用的无状态拦截器。</summary>
    public static SqliteConnectionPragmaInterceptor Instance { get; } = new();

    /// <summary>以弱引用记录已经完成配置的连接，不延长连接对象生命周期。</summary>
    private readonly ConditionalWeakTable<DbConnection, object> _configuredConnections = new();

    /// <summary>阻止外部创建多余实例。</summary>
    private SqliteConnectionPragmaInterceptor()
    {
    }

    /// <summary>连接首次打开后同步应用连接级参数。</summary>
    public override void ConnectionOpened(
        DbConnection connection,
        ConnectionEndEventData eventData)
    {
        ConfigureConnectionOnce(connection);
        base.ConnectionOpened(connection, eventData);
    }

    /// <summary>连接首次异步打开后应用连接级参数。</summary>
    public override Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ConfigureConnectionOnce(connection);
        return base.ConnectionOpenedAsync(
            connection,
            eventData,
            cancellationToken);
    }

    /// <summary>在单个连接对象上只执行一次低开销 PRAGMA 配置。</summary>
    private void ConfigureConnectionOnce(DbConnection connection)
    {
        lock (connection)
        {
            if (_configuredConnections.TryGetValue(connection, out _))
            {
                return;
            }

            using var command = connection.CreateCommand();
            command.CommandText = """
                PRAGMA synchronous=NORMAL;
                PRAGMA busy_timeout=30000;
                PRAGMA wal_autocheckpoint=1000;
                PRAGMA temp_store=MEMORY;
                PRAGMA cache_size=-32768;
                PRAGMA mmap_size=268435456;
                PRAGMA foreign_keys=ON;
                """;
            command.ExecuteNonQuery();
            _configuredConnections.Add(connection, new object());
        }
    }
}
