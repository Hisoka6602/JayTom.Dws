using JayTom.Dws.Models.Package;
using JayTom.Dws.Infrastructure;
using JayTom.Dws.Infrastructure.Configuration;
using JayTom.Dws.Infrastructure.DependencyInjection;
using JayTom.Dws.Infrastructure.Migrations;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.Application.Deployment;
using JayTom.Dws.Application.PackageHistory;
using JayTom.Dws.Tests.TestDoubles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

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
            Assert.Equal(5L, Convert.ToInt64(await command.ExecuteScalarAsync()));
        }
        finally {
            DeleteDatabaseArtifacts(databasePath);
        }
    }

    /// <summary>验证迁移会把旧库 WAL 中的已提交记录合并进稳定目录，同时保留源文件。</summary>
    [Fact]
    public async Task LegacyDatabaseMigration_CopiesCommittedWalDataAndKeepsSource() {
        var migrationRoot = Path.Combine(
            Path.GetTempPath(),
            $"jaytom-dws-location-migration-{Guid.NewGuid():N}");
        var legacyDirectory = Path.Combine(migrationRoot, "legacy");
        var stableDirectory = Path.Combine(migrationRoot, "stable");
        var sourcePath = Path.Combine(legacyDirectory, "Data.db");
        var targetPath = Path.Combine(stableDirectory, "Data.db");
        Directory.CreateDirectory(legacyDirectory);

        try {
            await using var writer = new SqliteConnection(
                $"Data Source={sourcePath};Mode=ReadWriteCreate;Pooling=False");
            await writer.OpenAsync();
            await using (var command = writer.CreateCommand()) {
                command.CommandText = """
                    PRAGMA journal_mode=WAL;
                    PRAGMA wal_autocheckpoint=0;
                    CREATE TABLE HistoricalPackages (Id INTEGER PRIMARY KEY, Barcode TEXT NOT NULL);
                    INSERT INTO HistoricalPackages (Id, Barcode) VALUES (1, 'HISTORY-001');
                    """;
                await command.ExecuteNonQueryAsync();
            }

            LegacyDatabaseMigrationCoordinator.EnsureMigrated(
                targetPath,
                "Data.db",
                [legacyDirectory]);

            Assert.True(File.Exists(sourcePath));
            Assert.True(File.Exists(targetPath));
            await using var migrated = new SqliteConnection(
                $"Data Source={targetPath};Mode=ReadOnly;Pooling=False");
            await migrated.OpenAsync();
            await using var valueCommand = migrated.CreateCommand();
            valueCommand.CommandText = "SELECT Barcode FROM HistoricalPackages WHERE Id = 1;";
            Assert.Equal("HISTORY-001", Convert.ToString(await valueCommand.ExecuteScalarAsync()));
            await using var integrityCommand = migrated.CreateCommand();
            integrityCommand.CommandText = "PRAGMA quick_check;";
            Assert.Equal("ok", Convert.ToString(await integrityCommand.ExecuteScalarAsync()));
        }
        finally {
            if (Directory.Exists(migrationRoot)) {
                Directory.Delete(migrationRoot, recursive: true);
            }
        }
    }

    /// <summary>验证稳定目录已经有数据库时，迁移逻辑绝不会覆盖现有数据。</summary>
    [Fact]
    public async Task LegacyDatabaseMigration_DoesNotOverwriteExistingTarget() {
        var migrationRoot = Path.Combine(
            Path.GetTempPath(),
            $"jaytom-dws-location-preserve-{Guid.NewGuid():N}");
        var legacyDirectory = Path.Combine(migrationRoot, "legacy");
        var stableDirectory = Path.Combine(migrationRoot, "stable");
        var sourcePath = Path.Combine(legacyDirectory, "Data.db");
        var targetPath = Path.Combine(stableDirectory, "Data.db");
        Directory.CreateDirectory(legacyDirectory);
        Directory.CreateDirectory(stableDirectory);

        try {
            await CreateMarkerDatabaseAsync(sourcePath, "legacy");
            await CreateMarkerDatabaseAsync(targetPath, "stable");

            LegacyDatabaseMigrationCoordinator.EnsureMigrated(
                targetPath,
                "Data.db",
                [legacyDirectory]);

            Assert.Equal("stable", await ReadMarkerAsync(targetPath));
            Assert.Equal("legacy", await ReadMarkerAsync(sourcePath));
        }
        finally {
            if (Directory.Exists(migrationRoot)) {
                Directory.Delete(migrationRoot, recursive: true);
            }
        }
    }

    /// <summary>验证旧记录缺少称重明细时仍可显示，无重量筛选不会误排除数据。</summary>
    [Fact]
    public async Task HistoryQuery_IncludesLegacyRecordWithoutWeight() {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"jaytom-dws-history-without-weight-{Guid.NewGuid():N}.db");

        try {
            var options = new DbContextOptionsBuilder<SqliteContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;
            await using (var context = new SqliteContext(options)) {
                context.Set<PackageInfoModel>().Add(new PackageInfoModel {
                    PackageTimestamped = 202608270001,
                    PackageCreateTime = new DateTime(2026, 8, 27, 10, 30, 0),
                    BarCodeInfo = new BarCodeInfoModel {
                        Barcode = "LEGACY-NO-WEIGHT",
                        ScanTime = new DateTime(2026, 8, 27, 10, 30, 0)
                    }
                });
                await context.SaveChangesAsync();
            }

            using var cache = new MemoryCache(new MemoryCacheOptions());
            var repository = new PackageRepository(
                new DelegateDbContextFactory<SqliteContext>(() => new SqliteContext(options)),
                cache);
            var service = new PackageHistoryQueryService(repository);

            var unfiltered = await service.SearchAsync(new PackageHistoryQuery(), 0, 20);
            var weightFiltered = await service.SearchAsync(
                new PackageHistoryQuery(MinWeight: 1),
                0,
                20);

            Assert.True(unfiltered.Items.Count == 1);
            var item = unfiltered.Items[0];
            Assert.Equal(1, unfiltered.Total);
            Assert.Equal("LEGACY-NO-WEIGHT", item.BarCodeInfo?.Barcode);
            Assert.Null(item.WeightInfo);
            Assert.Equal(0, weightFiltered.Total);
        }
        finally {
            DeleteDatabaseArtifacts(databasePath);
        }
    }

    /// <summary>验证持久化注册实际使用路径提供器给出的稳定数据目录。</summary>
    [Fact]
    public async Task PersistenceRegistration_UsesStableDataDirectory() {
        var applicationRoot = Path.Combine(
            Path.GetTempPath(),
            $"jaytom-dws-stable-path-{Guid.NewGuid():N}");
        var dataDirectory = Path.Combine(applicationRoot, "data");
        var paths = new DefaultApplicationPathProvider(new ApplicationPathOptions {
            DataDirectory = dataDirectory,
            ConfigurationDirectory = Path.Combine(applicationRoot, "configuration"),
            LogDirectory = Path.Combine(applicationRoot, "logs"),
            ModelDirectory = Path.Combine(applicationRoot, "models"),
            AdapterPackDirectory = Path.Combine(applicationRoot, "adapters")
        });

        try {
            var services = new ServiceCollection();
            services.AddSingleton<IApplicationPathProvider>(paths);
            services.AddDwsPersistence();
            await using var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IDbContextFactory<SqliteContext>>();
            await using var context = await factory.CreateDbContextAsync();

            Assert.Equal(
                Path.Combine(dataDirectory, "Data.db"),
                context.Database.GetDbConnection().DataSource);
        }
        finally {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(applicationRoot)) {
                Directory.Delete(applicationRoot, recursive: true);
            }
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

    /// <summary>创建只包含单个标记值的迁移测试数据库。</summary>
    private static async Task CreateMarkerDatabaseAsync(string databasePath, string marker) {
        await using var connection = new SqliteConnection(
            $"Data Source={databasePath};Mode=ReadWriteCreate;Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE Marker (Value TEXT NOT NULL); INSERT INTO Marker VALUES ($marker);";
        command.Parameters.AddWithValue("$marker", marker);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>读取迁移测试数据库中的标记值。</summary>
    private static async Task<string?> ReadMarkerAsync(string databasePath) {
        await using var connection = new SqliteConnection(
            $"Data Source={databasePath};Mode=ReadOnly;Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM Marker LIMIT 1;";
        return Convert.ToString(await command.ExecuteScalarAsync());
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
