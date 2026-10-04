using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JayTom.Dws.Infrastructure;

/// <summary>通过 SQLite 原生进度回调中断取消的历史查询，并在连接返回池之前清除回调。</summary>
internal sealed class SqliteReadCancellationScope : IDisposable
{
    /// <summary>只持有本次查询独占的已打开连接。</summary>
    private readonly SqliteConnection? _connection;

    /// <summary>打开查询连接并注册可中断全表统计的原生进度回调。</summary>
    public SqliteReadCancellationScope(DbContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context.Database.GetDbConnection() is not SqliteConnection connection) return;
        if (connection.State != ConnectionState.Open) context.Database.OpenConnection();
        _connection = connection;
        SQLitePCL.raw.sqlite3_progress_handler(connection.Handle, 1000,
            _ => cancellationToken.IsCancellationRequested ? 1 : 0, null);
    }

    /// <summary>只撤销本次查询回调，避免取消信号泄漏到池中复用的实时写连接。</summary>
    public void Dispose()
    {
        if (_connection?.State == ConnectionState.Open)
            SQLitePCL.raw.sqlite3_progress_handler(_connection.Handle, 0, null, null);
    }
}
