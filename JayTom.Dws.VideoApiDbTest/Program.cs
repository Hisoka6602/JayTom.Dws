using JayTom.Dws.Data.Package;
using JayTom.Dws.Infrastructure;
using JayTom.Dws.VideoApiDbTest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

internal class Program {

    private static void Main(string[] args) {
        var migrationSuccess = CreateMigration();
        if (migrationSuccess) {
            Console.WriteLine("Migration completed successfully.");
            SetIndexesToDescending();
        }
        else {
            Console.WriteLine("Migration failed. Check logs for details.");
        }

        Console.ReadLine();
    }

    public static void SetIndexesToDescending() {
        var dbContextFactory = new DesignTimeDbContextFactory();
        using var context = dbContextFactory.CreateDbContext(null);

        // 删除已有的索引（如果存在）
        /*context.Database.ExecuteSqlRaw(@"
        SET @sql = NULL;
        SELECT GROUP_CONCAT('DROP INDEX ', INDEX_NAME, ' ON ', TABLE_NAME) INTO @sql
        FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'Data_VideoBarCodeInfo'
        AND INDEX_NAME IN ('IX_Data_VideoBarCodeInfo_ScanTime', 'IX_Data_VideoBarCodeInfo_Barcode');

        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    ");*/

        // 创建新的索引
        context.Database.ExecuteSqlRaw(@"
        CREATE INDEX `IX_Data_VideoBarCodeInfo_ScanTime` ON `Data_VideoBarCodeInfo` (`ScanTime` DESC);
    ");

        context.Database.ExecuteSqlRaw(@"
        CREATE INDEX `IX_Data_VideoBarCodeInfo_Barcode` ON `Data_VideoBarCodeInfo` (`Barcode`(255));
    ");
    }

    public static bool CreateMigration() {
        var dbContextFactory = new DesignTimeDbContextFactory();
        try {
            using var context = dbContextFactory.CreateDbContext(null);
            context.Database.Migrate();
            return true; // Migration succeeded
        }
        catch (Exception ex) {
            Console.WriteLine($"Migration failed: {ex.Message}");
            return false; // Migration failed
        }
    }

    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<VideoApiContextDB> {

        public VideoApiContextDB CreateDbContext(string[] args) {
            /*var optionsBuilder = new DbContextOptionsBuilder<CloudApiContext1>();
            //f6vQDiiWpXLDUCxR
            optionsBuilder.UseMySql("Server=localhost;Port=3306;Password=f6vQDiiWpXLDUCxR;Database=CloudApi;User=root;",
                ServerVersion.AutoDetect("Server=localhost;Port=3306;Password=f6vQDiiWpXLDUCxR;Database=CloudApi;User=root;"),
            builder => {
                builder.SchemaBehavior(MySqlSchemaBehavior.Ignore);
            });
            return new CloudApiContext1(optionsBuilder.Options);*/
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

            var optionsBuilder = new DbContextOptionsBuilder<VideoApiContextDB>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), builder => {
                builder.SchemaBehavior(MySqlSchemaBehavior.Ignore);
            });

            return new VideoApiContextDB(optionsBuilder.Options);
        }
    }
}