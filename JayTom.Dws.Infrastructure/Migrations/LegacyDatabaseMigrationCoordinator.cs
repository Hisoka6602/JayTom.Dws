using System.Collections.Concurrent;
using Microsoft.Data.Sqlite;
using NLog;

namespace JayTom.Dws.Infrastructure.Migrations;

/// <summary>
/// 在数据库存储位置调整后，将旧位置中的 SQLite 数据库一次性迁移到当前目录。
/// </summary>
internal static class LegacyDatabaseMigrationCoordinator {
    /// <summary>允许部署脚本显式指定旧版数据库所在目录。</summary>
    private const string LegacyDataDirectoryEnvironmentVariable = "DWS_LEGACY_DATA_DIRECTORY";

    /// <summary>按目标数据库隔离并发迁移，防止多个上下文同时创建快照。</summary>
    private static readonly ConcurrentDictionary<string, object> MigrationLocks =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>记录不包含用户数据内容的迁移状态。</summary>
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 目标库不存在时，从旧安装位置备份出完整快照；旧文件始终保留且不会被修改。
    /// </summary>
    internal static void EnsureMigrated(string targetPath, string databaseFileName) =>
        EnsureMigrated(
            targetPath,
            databaseFileName,
            new[] {
                Environment.GetEnvironmentVariable(LegacyDataDirectoryEnvironmentVariable),
                GetPreviousStableDataDirectory(),
                AppContext.BaseDirectory,
                Environment.CurrentDirectory
            });

    /// <summary>使用指定旧目录执行迁移，供部署适配器与兼容性测试复用。</summary>
    internal static void EnsureMigrated(
        string targetPath,
        string databaseFileName,
        IEnumerable<string?> legacyDirectories) {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseFileName);
        ArgumentNullException.ThrowIfNull(legacyDirectories);

        var normalizedTargetPath = Path.GetFullPath(targetPath);
        var migrationLock = MigrationLocks.GetOrAdd(normalizedTargetPath, static _ => new object());
        lock (migrationLock) {
            if (File.Exists(normalizedTargetPath)) {
                return;
            }

            Directory.CreateDirectory(
                Path.GetDirectoryName(normalizedTargetPath) ??
                throw new InvalidOperationException("数据库目标目录无效。"));

            List<Exception>? failures = null;
            foreach (var sourcePath in FindLegacyCandidates(
                         databaseFileName,
                         normalizedTargetPath,
                         legacyDirectories)) {
                try {
                    BackupAndValidate(sourcePath, normalizedTargetPath);
                    Logger.Info(
                        "已将数据库 {DatabaseName} 从旧目录 {SourceDirectory} 迁移到当前数据库目录 {TargetDirectory}；旧文件已保留。",
                        databaseFileName,
                        Path.GetDirectoryName(sourcePath),
                        Path.GetDirectoryName(normalizedTargetPath));
                    return;
                }
                catch (Exception exception) {
                    failures ??= [];
                    failures.Add(exception);
                    Logger.Warn(
                        exception,
                        "旧版数据库 {DatabaseName} 从 {SourceDirectory} 迁移失败，将尝试其他候选位置。",
                        databaseFileName,
                        Path.GetDirectoryName(sourcePath));
                }
            }

            if (failures is { Count: > 0 }) {
                throw new AggregateException(
                    $"发现旧版 {databaseFileName}，但所有自动迁移尝试均失败。为避免用空库覆盖历史数据，应用已停止初始化。",
                    failures);
            }
        }
    }

    /// <summary>获取上一版本使用的稳定用户数据目录。</summary>
    private static string GetPreviousStableDataDirectory() {
        var applicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(applicationData, "JayTom", "Dws", "data");
    }

    /// <summary>枚举显式旧目录、上一版本用户数据目录和当前工作目录中的旧数据库。</summary>
    private static IEnumerable<string> FindLegacyCandidates(
        string databaseFileName,
        string normalizedTargetPath,
        IEnumerable<string?> directories) {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var directory in directories) {
            if (string.IsNullOrWhiteSpace(directory)) {
                continue;
            }

            string candidate;
            try {
                candidate = Path.GetFullPath(Path.Combine(directory, databaseFileName));
            }
            catch (Exception exception) when (
                exception is ArgumentException or NotSupportedException or PathTooLongException) {
                Logger.Warn(exception, "忽略无效的旧版数据库目录 {LegacyDirectory}。", directory);
                continue;
            }

            if (string.Equals(candidate, normalizedTargetPath, StringComparison.OrdinalIgnoreCase) ||
                !visited.Add(candidate) ||
                !IsUsableFile(candidate)) {
                continue;
            }

            yield return candidate;
        }
    }

    /// <summary>通过 SQLite 在线备份 API 合并 WAL 内容，再校验快照完整性并原子落盘。</summary>
    private static void BackupAndValidate(string sourcePath, string targetPath) {
        var temporaryPath = $"{targetPath}.migrating-{Guid.NewGuid():N}";
        try {
            using (var source = CreateConnection(sourcePath, SqliteOpenMode.ReadOnly))
            using (var destination = CreateConnection(
                       temporaryPath,
                       SqliteOpenMode.ReadWriteCreate)) {
                source.Open();
                destination.Open();
                source.BackupDatabase(destination);

                using var integrityCommand = destination.CreateCommand();
                integrityCommand.CommandText = "PRAGMA quick_check;";
                var integrityResult = Convert.ToString(integrityCommand.ExecuteScalar());
                if (!string.Equals(integrityResult, "ok", StringComparison.OrdinalIgnoreCase)) {
                    throw new InvalidDataException(
                        $"SQLite 完整性检查失败：{integrityResult ?? "无返回结果"}。");
                }
            }

            File.Move(temporaryPath, targetPath, overwrite: false);
        }
        finally {
            DeleteIfExists(temporaryPath);
            DeleteIfExists($"{temporaryPath}-wal");
            DeleteIfExists($"{temporaryPath}-shm");
        }
    }

    /// <summary>通过 SQLite 提供器工厂创建迁移专用连接，避免连接池持有临时文件。</summary>
    private static SqliteConnection CreateConnection(string databasePath, SqliteOpenMode mode) {
        var connection = (SqliteConnection)SqliteFactory.Instance.CreateConnection();
        connection.ConnectionString = new SqliteConnectionStringBuilder {
            DataSource = databasePath,
            Mode = mode,
            Cache = SqliteCacheMode.Private,
            Pooling = false,
            DefaultTimeout = 30
        }.ToString();
        return connection;
    }

    /// <summary>判断候选源文件是否真实存在且包含数据。</summary>
    private static bool IsUsableFile(string path) =>
        File.Exists(path) && new FileInfo(path).Length > 0;

    /// <summary>清理本次迁移产生但尚未落盘的临时文件。</summary>
    private static void DeleteIfExists(string path) {
        if (File.Exists(path)) {
            File.Delete(path);
        }
    }
}
