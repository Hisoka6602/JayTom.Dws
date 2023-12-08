using JayTom.Dws.Infrastructure;
using JayTom.Dws.Data.VideoApiData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

internal class Program {
    private static IConfigurationRoot _configuration;

    public static void Main(string[] args) {
        /*
        var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        _configuration = builder.Build();
        */

        CreateMigration();
        Console.WriteLine("Migration completed successfully.");
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
            optionsBuilder.UseSqlServer("data source=82.156.244.249;initial catalog=DwsVideoApi;persist security info=true;user id=sa;password=Yunshan2021+-/;Max Pool Size = 32767;Packet Size= 1024;Connect Timeout=10;TrustServerCertificate=true");

            return new VideoApiContext1(optionsBuilder.Options);
        }
    }

    public class VideoApiContext1 : DbContext {

        public VideoApiContext1(DbContextOptions<VideoApiContext1> options) : base(options) {
        }

        /*
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            optionsBuilder.UseSqlServer("data source=82.156.244.249;initial catalog=DwsVideoApi;persist security info=true;user id=sa;password=Yunshan2021+-/;Max Pool Size = 32767;Packet Size= 1024;Connect Timeout=10;TrustServerCertificate=true");
        }
        */

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<VideoBarCodeInfoModel>().HasKey(c => new {
                c.Id
            });
            modelBuilder.Entity<VideoNodeImageInfoModel>().HasKey(c => new {
                c.Id
            });
            modelBuilder.Entity<VideoScanNodeInfoModel>().HasKey(c => new {
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
        }
    }
}