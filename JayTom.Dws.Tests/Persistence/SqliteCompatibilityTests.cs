using JayTom.Dws.Models.Package;
using JayTom.Dws.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JayTom.Dws.Tests.Persistence;

/// <summary>验证既有 SQLite 文件可在 long 与 decimal 业务语义下原位使用。</summary>
public sealed class SqliteCompatibilityTests {
    /// <summary>验证全新空库可建立基线结构并登记版本化迁移。</summary>
    [Fact]
    public async Task EmptyDatabase_BootstrapsSchemaBeforeApplyingMigrations() {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"jaytom-dws-empty-{Guid.NewGuid():N}.db");

        try {
            var options = new DbContextOptionsBuilder<SqliteContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;
            await using (var context = new SqliteContext(options)) {
                Assert.True(await context.Set<PackageInfoModel>().AnyAsync() == false);
            }

            await using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory;";
            Assert.Equal(4L, Convert.ToInt64(await command.ExecuteScalarAsync()));
        }
        finally {
            DeleteDatabaseArtifacts(databasePath);
        }
    }

    /// <summary>验证旧 INTEGER/REAL 列可读写且初始化不会重建业务表。</summary>
    [Fact]
    public async Task LegacyDatabase_RemainsStructurallyCompatible() {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"jaytom-dws-compatibility-{Guid.NewGuid():N}.db");

        try {
            await CreateLegacyDatabaseAsync(databasePath);
            var originalTableDefinition = await ReadWeightTableDefinitionAsync(databasePath);

            var options = new DbContextOptionsBuilder<SqliteContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            await using (var context = new SqliteContext(options)) {
                var weight = await context.Set<WeightInfoModel>().SingleAsync();
                Assert.Equal(5_000_000_000L, weight.Id);
                Assert.Equal(12345.6789m, weight.FormattedWeight);

                weight.FormattedWeight = 9876.5432m;
                await context.SaveChangesAsync();
            }

            var currentTableDefinition = await ReadWeightTableDefinitionAsync(databasePath);
            Assert.Equal(originalTableDefinition, currentTableDefinition);

            await using var verificationConnection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
            await verificationConnection.OpenAsync();

            await using var typeCommand = verificationConnection.CreateCommand();
            typeCommand.CommandText = "PRAGMA table_info('Data_WeightInfo');";
            await using var reader = await typeCommand.ExecuteReaderAsync();
            var columnTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (await reader.ReadAsync()) {
                columnTypes[reader.GetString(1)] = reader.GetString(2);
            }

            Assert.Equal("INTEGER", columnTypes["Id"]);
            Assert.Equal("INTEGER", columnTypes["PackageId"]);
            Assert.Equal("REAL", columnTypes["FormattedWeight"]);
            await reader.DisposeAsync();

            await using var valueCommand = verificationConnection.CreateCommand();
            valueCommand.CommandText = "SELECT FormattedWeight FROM Data_WeightInfo WHERE Id = 5000000000;";
            var storedValue = Convert.ToDecimal(await valueCommand.ExecuteScalarAsync());
            Assert.Equal(9876.5432m, storedValue);

            await using var migrationCommand = verificationConnection.CreateCommand();
            migrationCommand.CommandText = """
                SELECT COUNT(*)
                FROM __EFMigrationsHistory
                WHERE MigrationId IN (
                    '202608110001_FixedPointCompatibility',
                    '202608110002_ModelSemanticsCompatibility');
                """;
            Assert.Equal(2L, Convert.ToInt64(await migrationCommand.ExecuteScalarAsync()));
        }
        finally {
            DeleteDatabaseArtifacts(databasePath);
        }
    }

    /// <summary>创建与已落地文件一致的 INTEGER/REAL 旧式数据库片段。</summary>
    private static async Task CreateLegacyDatabaseAsync(string databasePath) {
        await using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE Data_WeightInfo (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                PackageId INTEGER NOT NULL,
                SourceType INTEGER NOT NULL,
                OriginalText TEXT NOT NULL,
                FormattedWeight REAL NOT NULL,
                CreateTime TEXT NOT NULL,
                WeighingMode INTEGER NOT NULL
            );
            CREATE TABLE Data_BarCodeInfo (Id INTEGER PRIMARY KEY, ScanTime TEXT);
            CREATE TABLE Data_ExitInfo (Id INTEGER PRIMARY KEY, PhysicalExit TEXT);
            CREATE TABLE Data_UploadInfo (Id INTEGER PRIMARY KEY, RequestStatus INTEGER);
            INSERT INTO Data_WeightInfo
                (Id, PackageId, SourceType, OriginalText, FormattedWeight, CreateTime, WeighingMode)
            VALUES
                (5000000000, 4000000000, 1, '12345.6789', 12345.6789, '2026-08-11 12:00:00', 1);
            """;
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>读取重量表的原始建表定义。</summary>
    private static async Task<string> ReadWeightTableDefinitionAsync(string databasePath) {
        await using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Data_WeightInfo';";
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? string.Empty;
    }

    /// <summary>删除当前测试创建的数据库及 SQLite 辅助文件。</summary>
    private static void DeleteDatabaseArtifacts(string databasePath) {
        foreach (var path in new[] { databasePath, $"{databasePath}-wal", $"{databasePath}-shm" }) {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }
}
