using JayTom.Dws.Data.License;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Data.LocalData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

internal class Program {

    private static void Main(string[] args) {
        CreateMigration();
        Console.WriteLine("Migration completed successfully.");
        Console.ReadLine();
        Console.WriteLine("Hello, World!");
    }

    public static void CreateMigration() {
        var dbContextFactory = new DesignTimeDbContextFactory();
        using (var context = dbContextFactory.CreateDbContext(null)) {
            context.Database.Migrate();
        }
    }

    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CloudApiContext1> {

        public CloudApiContext1 CreateDbContext(string[] args) {
            var optionsBuilder = new DbContextOptionsBuilder<CloudApiContext1>();
            //f6vQDiiWpXLDUCxR
            optionsBuilder.UseMySql("Server=localhost;Port=3306;Password=f6vQDiiWpXLDUCxR;Database=CloudApi;User=root;",
                ServerVersion.AutoDetect("Server=localhost;Port=3306;Password=f6vQDiiWpXLDUCxR;Database=CloudApi;User=root;"),
            builder => {
                builder.SchemaBehavior(MySqlSchemaBehavior.Ignore);
            });
            return new CloudApiContext1(optionsBuilder.Options);
        }
    }

    public class CloudApiContext1 : DbContext {

        public CloudApiContext1(DbContextOptions<CloudApiContext1> options) : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            //data
            {
                modelBuilder.Entity<PackageInfoModel>()
                    .HasKey(c => new {
                        c.Id
                    });
                modelBuilder.Entity<PackageInfoModel>()
                    .HasIndex(b => b.PackageTimestamped)
                    .IsUnique(false);
                modelBuilder.Entity<PackageInfoModel>()
                    .HasIndex(b => b.PackageCreateTime)
                    .IsUnique(false)
                    .HasAnnotation("IndexSortOrder", "Descending");
                //条码信息
                modelBuilder.Entity<PackageInfoModel>()
                    .HasOne(b => b.BarCodeInfo)
                    .WithOne(n => n.PackageInfo)
                    .HasForeignKey<BarCodeInfoModel>(n => n.PackageId)
                    .OnDelete(DeleteBehavior.Cascade);

                modelBuilder.Entity<BarCodeInfoModel>()
                    .HasKey(c => new {
                        c.Id
                    });
                modelBuilder.Entity<BarCodeInfoModel>()
                    .HasIndex(b => b.Barcode)
                    .IsUnique(false);
                modelBuilder.Entity<BarCodeInfoModel>()
                    .HasIndex(b => b.ScanTime)
                    .IsUnique(false)
                    .HasAnnotation("IndexSortOrder", "Descending");
                //称重信息
                modelBuilder.Entity<PackageInfoModel>()
                    .HasOne(b => b.WeightInfo)
                    .WithOne(n => n.PackageInfo)
                    .HasForeignKey<WeightInfoModel>(n => n.PackageId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<WeightInfoModel>().HasKey(c => new {
                    c.Id
                });
                //体积信息
                modelBuilder.Entity<PackageInfoModel>()
                    .HasOne(b => b.VolumeInfo)
                    .WithOne(n => n.PackageInfo)
                    .HasForeignKey<VolumeInfoModel>(n => n.PackageId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<VolumeInfoModel>().HasKey(c => new {
                    c.Id
                });

                //上传信息
                modelBuilder.Entity<PackageInfoModel>()
                    .HasOne(b => b.UploadInfo)
                    .WithOne(n => n.PackageInfo)
                    .HasForeignKey<UploadInfoModel>(n => n.PackageId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<UploadInfoModel>().HasKey(c => new {
                    c.Id
                });

                //格口信息
                modelBuilder.Entity<PackageInfoModel>()
                    .HasOne(b => b.ExitInfo)
                    .WithOne(n => n.PackageInfo)
                    .HasForeignKey<ExitInfoModel>(n => n.PackageId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<ExitInfoModel>().HasKey(c => new {
                    c.Id
                });
                //分拣信息
                modelBuilder.Entity<PackageInfoModel>()
                    .HasOne(b => b.SortingInfo)
                    .WithOne(n => n.PackageInfo)
                    .HasForeignKey<SortingInfoModel>(n => n.PackageId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<SortingInfoModel>().HasKey(c => new {
                    c.Id
                });

                //物流信息
                modelBuilder.Entity<PackageInfoModel>()
                    .HasOne(b => b.LogisticsInfo)
                    .WithOne(n => n.PackageInfo)
                    .HasForeignKey<LogisticsInfoModel>(n => n.PackageId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<LogisticsInfoModel>().HasKey(c => new {
                    c.Id
                });
                //Ocr
                modelBuilder.Entity<PackageInfoModel>()
                    .HasOne(b => b.OcrInfo)
                    .WithOne(n => n.PackageInfo)
                    .HasForeignKey<OcrInfoModel>(n => n.PackageId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<OcrInfoModel>().HasKey(c => new {
                    c.Id
                });
                //Ocr详细信息
                modelBuilder.Entity<OcrInfoModel>()
                    .HasMany(b => b.OcrDetailedInfos)
                    .WithOne(n => n.OcrInfo)
                    .HasForeignKey(n => new { n.OcrInfoId })
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<OcrDetailedInfoModel>().HasKey(c => new {
                    c.Id
                });
                //图片信息
                modelBuilder.Entity<PackageInfoModel>()
                    .HasMany(b => b.ImageInfos)
                    .WithOne(n => n.PackageInfo)
                    .HasForeignKey(n => new { n.PackageId })
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<ImageInfoModel>().HasKey(c => new {
                    c.Id
                });
                //设备
                modelBuilder.Entity<PackageInfoModel>()
                    .HasOne(b => b.DeviceInfo)
                    .WithOne(n => n.PackageInfo)
                    .HasForeignKey<DeviceInfoModel>(n => n.PackageId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<DeviceInfoModel>().HasKey(c => new {
                    c.Id
                });
                //聚合包裹信息
                modelBuilder.Entity<PackageInfoModel>()
                    .HasOne(b => b.AggregatePackagesInfo)
                    .WithOne(n => n.PackageInfo)
                    .HasForeignKey<AggregatePackagesInfoModel>(n => n.PackageId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<AggregatePackagesInfoModel>().HasKey(c => new {
                    c.Id
                });
                //指令信息

                modelBuilder.Entity<SortingInfoModel>()
                    .HasMany(b => b.InstructionInfos)
                    .WithOne(n => n.SortingInfo)
                    .HasForeignKey(n => new { n.SortingInfoId })
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<InstructionInfoModel>().HasKey(c => new {
                    c.Id
                });
            }
        }
    }
}