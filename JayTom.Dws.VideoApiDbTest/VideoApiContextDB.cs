using JayTom.Dws.Data.Package;
using JayTom.Dws.Data.VideoApiData;
using Microsoft.EntityFrameworkCore;
using JayTom.Dws.Data.LocalConf.CloudConfig;

namespace JayTom.Dws.VideoApiDbTest {

    public class VideoApiContextDb : DbContext {

        public VideoApiContextDb(DbContextOptions<VideoApiContextDb> options) : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
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
            /*modelBuilder.Entity<BarCodeInfoModel>()
                .HasIndex(b => b.Barcode)
                .IsUnique(false)
                .HasAnnotation("IndexSortOrder", "Descending");
            modelBuilder.Entity<BarCodeInfoModel>()
                .HasIndex(b => b.ScanTime)
                .IsUnique(false)
                .HasAnnotation("IndexSortOrder", "Descending");*/

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

            //Nvr
            modelBuilder.Entity<PackageInfoModel>()
                .HasMany(b => b.NvrInfos)
                .WithOne(n => n.PackageInfo)
                .HasForeignKey(n => new { n.PackageId })
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<NvrInfoModel>().HasKey(c => new {
                c.Id
            });
        }
    }
}