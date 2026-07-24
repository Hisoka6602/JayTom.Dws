using System.Data;
using Microsoft.EntityFrameworkCore;

namespace JayTom.Dws.Infrastructure {

    /// <summary>
    /// 负责对 SQLite 数据库执行一次性初始化和持久化性能设置。
    /// </summary>
    internal static class SqliteDatabaseInitializer {

        /// <summary>
        /// 确保指定类型的数据库只初始化一次。
        /// </summary>
        /// <typeparam name="TContext">数据库上下文类型。</typeparam>
        /// <param name="context">用于初始化数据库的上下文。</param>
        /// <param name="databasePath">数据库文件的绝对路径。</param>
        public static void EnsureInitialized<TContext>(
            TContext context,
            string databasePath) where TContext : DbContext {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

            var state = InitializationStateCache<TContext>.Value;
            if (Volatile.Read(ref state.IsInitialized) == 1) {
                return;
            }

            lock (state.SyncRoot) {
                if (state.IsInitialized == 1) {
                    return;
                }

                // 当前项目没有 EF 迁移，EnsureCreated 会直接按模型创建缺失的数据库。
                context.Database.EnsureCreated();
                ConfigurePersistentSettings(context, databasePath);
                Volatile.Write(ref state.IsInitialized, 1);
            }
        }

        /// <summary>
        /// 配置兼顾读取吞吐和断电安全的 SQLite 持久化选项。
        /// </summary>
        /// <param name="context">用于执行设置的数据库上下文。</param>
        /// <param name="databasePath">数据库文件的绝对路径。</param>
        private static void ConfigurePersistentSettings(DbContext context, string databasePath) {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;
            if (shouldClose) {
                connection.Open();
            }

            try {
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA optimize;";
                command.ExecuteNonQuery();
                if (string.Equals(
                        Path.GetFileName(databasePath),
                        "Data.db",
                        StringComparison.OrdinalIgnoreCase)) {
                    CreateDataReadIndexes(connection);
                }
            }
            finally {
                if (shouldClose) {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// 为数据管理页面的高频筛选条件创建读取索引。
        /// </summary>
        /// <param name="connection">已经打开的 SQLite 连接。</param>
        private static void CreateDataReadIndexes(System.Data.Common.DbConnection connection) {
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE INDEX IF NOT EXISTS IX_Data_BarCodeInfo_ScanTime
                    ON Data_BarCodeInfo (ScanTime);
                CREATE INDEX IF NOT EXISTS IX_Data_WeightInfo_FormattedWeight
                    ON Data_WeightInfo (FormattedWeight);
                CREATE INDEX IF NOT EXISTS IX_Data_ExitInfo_PhysicalExit
                    ON Data_ExitInfo (PhysicalExit);
                CREATE INDEX IF NOT EXISTS IX_Data_UploadInfo_RequestStatus
                    ON Data_UploadInfo (RequestStatus);
                """;
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 表示单个数据库类型的一次性初始化状态。
        /// </summary>
        private sealed class InitializationState {

            /// <summary>
            /// 获取数据库初始化过程使用的同步对象。
            /// </summary>
            public object SyncRoot { get; } = new();

            /// <summary>
            /// 表示初始化是否已经成功完成。
            /// </summary>
            public int IsInitialized;
        }

        /// <summary>
        /// 为每种数据库上下文保存独立的一次性初始化状态。
        /// </summary>
        /// <typeparam name="TContext">数据库上下文类型。</typeparam>
        private static class InitializationStateCache<TContext> where TContext : DbContext {

            /// <summary>
            /// 获取当前上下文类型共享的初始化状态。
            /// </summary>
            public static readonly InitializationState Value = new();
        }
    }
}
