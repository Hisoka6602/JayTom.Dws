using JayTom.Dws.Infrastructure;
using JayTom.Dws.Data.VideoApiData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

internal class Program {
    private static IConfigurationRoot _configuration;
    private static string _connectionString = string.Empty;

    public static void Main(string[] args) {
        /*var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        var configuration = builder.Build();
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;*/
        CreateMigration();
        Console.WriteLine("Migration completed successfully.");
        Console.ReadLine();
    }

    public static void CreateMigration() {
        var dbContextFactory = new DesignTimeDbContextFactory();
        using (var context = dbContextFactory.CreateDbContext(null)) {
            context.Database.Migrate();
        }
    }

    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<VideoApiContext1> {

        public VideoApiContext1 CreateDbContext(string[] args) {
            var optionsBuilder = new DbContextOptionsBuilder<VideoApiContext1>();

            optionsBuilder.UseMySql("Server=localhost;Port=3306;Password=4cW3Ld4KNYWaF8M3;Database=dh;User=DH;",
                ServerVersion.AutoDetect("Server=localhost;Port=3306;Password=4cW3Ld4KNYWaF8M3;Database=dh;User=DH;"),
                builder => {
                    builder.SchemaBehavior(MySqlSchemaBehavior.Ignore);
                });
            return new VideoApiContext1(optionsBuilder.Options);
        }
    }

    public class VideoApiContext1 : DbContext {

        public VideoApiContext1(DbContextOptions<VideoApiContext1> options) : base(options) {
        }

        /*protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            optionsBuilder.UseSqlServer("Server=127.0.0.1;Port=3306;Password=f6vQDiiWpXLDUCxR;Database=dh;User=root;");
        }*/

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema(null);
            modelBuilder.Entity<VideoBarCodeInfoModel>().HasKey(c => new {
                c.Id
            });
            modelBuilder.Entity<VideoNodeImageInfoModel>().HasKey(c => new {
                c.Id
            });
            modelBuilder.Entity<VideoScanNodeInfoModel>().HasKey(c => new {
                c.Id
            });
            modelBuilder.Entity<VideoNvrCameraBindingInfoModel>().HasKey(c => new {
                c.Id
            });
            //配置对应关系

            modelBuilder.Entity<VideoBarCodeInfoModel>()
                .HasMany(b => b.VideoScanNodeInfos)
                .WithOne(n => n.BarCodeInfo)
                .HasForeignKey(n => new { n.BarcodeId })
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VideoScanNodeInfoModel>()
                .HasMany(b => b.VideoNodeImageInfos)
                .WithOne(n => n.ScanNodeInfo)
                .HasForeignKey(n => new { n.ScanNodeId })
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VideoScanNodeInfoModel>()
                .HasOne(b => b.VideoNvrCameraBindingInfo)
                .WithOne(n => n.ScanNodeInfo)
                .HasForeignKey<VideoNvrCameraBindingInfoModel>(n => n.ScanNodeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}