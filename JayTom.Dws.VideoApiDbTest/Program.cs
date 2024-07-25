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
        // 创建新的索引
        context.Database.ExecuteSqlRaw(@"
        CREATE INDEX `IX_PackageInfoModel_PackageCreateTime` ON `Data_PackageInfo` (`PackageCreateTime` DESC);
    ");

        context.Database.ExecuteSqlRaw(@"
        CREATE INDEX `IX_BarCodeInfoModel_Barcode` ON `Data_BarCodeInfo` (`Barcode`(50));
    ");

        context.Database.ExecuteSqlRaw(@"
        CREATE INDEX `IX_BarCodeInfoModel_ScanTime` ON `Data_BarCodeInfo` (`ScanTime` DESC);
    ");

        context.Database.ExecuteSqlRaw(@"
        CREATE INDEX `IX_DeviceInfoModel_NodeName` ON `Data_DeviceInfo` (`NodeName`(50));
    ");

        context.Database.ExecuteSqlRaw(@"
        CREATE INDEX `IX_DeviceInfoModel_DeviceName` ON `Data_DeviceInfo` (`DeviceName`(50));
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

    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<VideoApiContextDb> {

        public VideoApiContextDb CreateDbContext(string[] args) {
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

            var optionsBuilder = new DbContextOptionsBuilder<VideoApiContextDb>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), builder => {
                builder.SchemaBehavior(MySqlSchemaBehavior.Ignore);
            });

            return new VideoApiContextDb(optionsBuilder.Options);
        }
    }
}