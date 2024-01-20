using JayTom.Dws.Data.Package;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalData;
using JayTom.Dws.Data.LocalConf;
using Microsoft.EntityFrameworkCore;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;

namespace JayTom.Dws.Infrastructure {

    public sealed class SqliteContext : DbContext {

        public SqliteContext(DbContextOptions<SqliteContext> options) : base(options) {
            lock (System.AppDomain.CurrentDomain.BaseDirectory) {
                var s = $"{System.AppDomain.CurrentDomain.BaseDirectory}Data.db";
                if (!File.Exists(s)) {
                    Database.EnsureCreated();
                    Database.Migrate();
                }
                else {
                    if (Database.GetPendingMigrations().Any()) {
                        Database.Migrate(); //执行迁移
                    }
                }
            }
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
                //视频云
                modelBuilder.Entity<PackageInfoModel>()
                    .HasOne(b => b.CloudVideoUploadInfo)
                    .WithOne(n => n.PackageInfo)
                    .HasForeignKey<CloudVideoUploadInfoModel>(n => n.PackageId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<CloudVideoUploadInfoModel>().HasKey(c => new {
                    c.Id
                });

                modelBuilder.Entity<SoundInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<SoundInfoModel>()
                    .HasIndex(b => b.SoundName)
                    .IsUnique();
            }

            base.OnModelCreating(modelBuilder);
        }
    }
}