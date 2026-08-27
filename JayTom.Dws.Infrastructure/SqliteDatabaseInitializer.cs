using System.Data;
using System.Collections.Concurrent;
using JayTom.Dws.Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore;

namespace JayTom.Dws.Infrastructure {

    /// <summary>
    /// 负责对 SQLite 数据库执行一次性初始化和持久化性能配置。
    /// </summary>
    internal static class SqliteDatabaseInitializer {
        /// <summary>
        /// 按上下文类型和数据库绝对路径分别保存初始化状态。
        /// </summary>
        private static readonly ConcurrentDictionary<string, InitializationState> InitializationStates = new();

        /// <summary>解析上下文当前实际连接的数据库路径。</summary>
        /// <param name="context">数据库上下文。</param>
        /// <param name="fallbackFileName">连接未提供数据源时使用的文件名。</param>
        /// <returns>文件数据库的绝对路径，或内存数据库的上下文级标识。</returns>
        public static string ResolveDatabasePath(DbContext context, string fallbackFileName) {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentException.ThrowIfNullOrWhiteSpace(fallbackFileName);

            var dataSource = context.Database.GetDbConnection().DataSource;
            if (string.IsNullOrWhiteSpace(dataSource)) {
                dataSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fallbackFileName);
            }

            if (string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase)) {
                return $":memory:|{context.ContextId.InstanceId}";
            }

            return Path.GetFullPath(dataSource);
        }

        /// <summary>
        /// 确保指定类型和路径的数据库只初始化一次。
        /// </summary>
        /// <typeparam name="TContext">数据库上下文类型。</typeparam>
        /// <param name="context">用于初始化数据库的上下文。</param>
        /// <param name="databasePath">数据库文件的绝对路径。</param>
        public static void EnsureInitialized<TContext>(
            TContext context,
            string databasePath) where TContext : DbContext {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

            var stateKey = $"{typeof(TContext).AssemblyQualifiedName}|{databasePath}";
            var state = InitializationStates.GetOrAdd(stateKey, _ => new InitializationState());
            if (Volatile.Read(ref state.IsInitialized) == 1) {
                return;
            }

            lock (state.SyncRoot) {
                if (state.IsInitialized == 1) {
                    return;
                }

                // 空库只引导一次当前基线；既有数据库的任何演进均由版本化迁移负责。
                SqliteSchemaMigrator.Apply(context);
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
                command.CommandText = """
                    PRAGMA journal_mode=WAL;
                    PRAGMA synchronous=NORMAL;
                    PRAGMA busy_timeout=30000;
                    PRAGMA wal_autocheckpoint=1000;
                    PRAGMA temp_store=MEMORY;
                    PRAGMA cache_size=-32768;
                    PRAGMA mmap_size=268435456;
                    PRAGMA optimize;
                    """;
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
                CREATE INDEX IF NOT EXISTS IX_Data_PackageInfo_PackageTimestamped
                    ON Data_PackageInfo (PackageTimestamped);
                CREATE INDEX IF NOT EXISTS IX_Data_InstructionInfo_SortingInfoId
                    ON Data_InstructionInfo (SortingInfoId);
                CREATE INDEX IF NOT EXISTS IX_Data_SortingInfo_PackageId
                    ON Data_SortingInfo (PackageId);
                CREATE INDEX IF NOT EXISTS IX_Data_ExitInfo_PackageId
                    ON Data_ExitInfo (PackageId);
                CREATE INDEX IF NOT EXISTS IX_Data_ImageInfo_PackageId
                    ON Data_ImageInfo (PackageId);
                """;
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 表示单个数据库类型和路径的一次性初始化状态。
        /// </summary>
        private sealed class InitializationState {

            /// <summary>
            /// 获取数据库初始化过程使用的同步对象。
            /// </summary>
            public System.Threading.Lock SyncRoot { get; } = new();

            /// <summary>
            /// 表示初始化是否已经成功完成。
            /// </summary>
            public int IsInitialized;
        }
    }
}
