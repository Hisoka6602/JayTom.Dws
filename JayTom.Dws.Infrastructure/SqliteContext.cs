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
                modelBuilder.Entity<BarCodeInfoModel>()
                    .HasKey(c => new {
                        c.Id
                    });

                modelBuilder.Entity<BarCodeInfoModel>()
                    .HasIndex(b => b.Barcode)
                    .IsUnique(false);
                modelBuilder.Entity<BarCodeInfoModel>()
                    .HasIndex(b => b.TimestampedGuid)
                    .IsUnique(false);
                modelBuilder.Entity<BarCodeInfoModel>()
                    .HasIndex(b => b.ScanTime)
                    .IsUnique(false)
                    .HasAnnotation("IndexSortOrder", "Descending");
                //图片信息
                modelBuilder.Entity<BarCodeInfoModel>()
                    .HasMany(b => b.ImageInfos)
                    .WithOne(n => n.BarCodeInfo)
                    .HasForeignKey(n => new { n.BarcodeId })
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<ImageInfoModel>().HasKey(c => new {
                    c.Id
                });
                /*modelBuilder.Entity<PanoramaImageInfoModel>().HasKey(c => new {
                    c.Id
                });*/
                //体积信息
                modelBuilder.Entity<BarCodeInfoModel>()
                    .HasOne(b => b.VolumeInfo)
                    .WithOne(n => n.BarCodeInfo)
                    .HasForeignKey<VolumeInfoModel>(n => n.BarcodeId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<VolumeInfoModel>().HasKey(c => new {
                    c.Id
                });
                //称重信息
                modelBuilder.Entity<BarCodeInfoModel>()
                    .HasOne(b => b.WeightInfo)
                    .WithOne(n => n.BarCodeInfo)
                    .HasForeignKey<WeightInfoModel>(n => n.BarcodeId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<WeightInfoModel>().HasKey(c => new {
                    c.Id
                });
                //上传信息
                modelBuilder.Entity<BarCodeInfoModel>()
                    .HasOne(b => b.UploadInfo)
                    .WithOne(n => n.BarCodeInfo)
                    .HasForeignKey<UploadInfoModel>(n => n.BarcodeId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<UploadInfoModel>().HasKey(c => new {
                    c.Id
                });
                //分拣信息
                modelBuilder.Entity<BarCodeInfoModel>()
                    .HasOne(b => b.SortingInfo)
                    .WithOne(n => n.BarCodeInfo)
                    .HasForeignKey<SortingInfoModel>(n => n.BarcodeId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<SortingInfoModel>().HasKey(c => new {
                    c.Id
                });
                //Ocr
                modelBuilder.Entity<BarCodeInfoModel>()
                    .HasOne(b => b.OcrInfo)
                    .WithOne(n => n.BarCodeInfo)
                    .HasForeignKey<OcrInfoModel>(n => n.BarcodeId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<OcrInfoModel>().HasKey(c => new {
                    c.Id
                });
                //视频云
                modelBuilder.Entity<BarCodeInfoModel>()
                    .HasOne(b => b.CloudVideoUploadInfo)
                    .WithOne(n => n.BarCodeInfo)
                    .HasForeignKey<CloudVideoUploadInfoModel>(n => n.BarcodeId)
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