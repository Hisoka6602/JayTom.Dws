using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Data.ServerData;
using JayTom.Dws.Data.VideoApiData;
using Microsoft.EntityFrameworkCore;
using JayTom.Dws.Data.LocalConf.CloudConfig;

namespace JayTom.Dws.Infrastructure {

    public class VideoApiContext : DbContext {

        public VideoApiContext(DbContextOptions<VideoApiContext> options) : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            // 配置基类的主键
            /*modelBuilder.Entity<NvrCameraBindingInfoModel>()
                .HasKey(c => c.Id);
            modelBuilder.Entity<VideoBarCodeInfoModel>().HasKey(c => new {
                c.Id
            });
            modelBuilder.Entity<VideoNodeImageInfoModel>().HasKey(c => new {
                c.Id
            });
            modelBuilder.Entity<VideoScanNodeInfoModel>().HasKey(c => new {
                c.Id
            });
            modelBuilder.Entity<VideoNvrCameraBindingInfoModel>()
                .HasBaseType<NvrCameraBindingInfoModel>();  // 配置继承关系
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
                .HasMany(b => b.VideoNvrCameraBindingInfos)
                .WithOne(n => n.ScanNodeInfo)
                .HasForeignKey(n => n.ScanNodeId)
                .OnDelete(DeleteBehavior.Cascade);*/

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

            //基础配置
            modelBuilder.Entity<ConfigInfoModel>().HasKey(c => new {
                c.Id
            });
            modelBuilder.Entity<ConfigInfoModel>()
                .HasIndex(b => b.ConfigName)
                .IsUnique();
        }
    }
}