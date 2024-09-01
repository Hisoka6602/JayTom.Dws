using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Data.LocalData;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Data.CloudApiData;
using Microsoft.EntityFrameworkCore;

namespace JayTom.Dws.Infrastructure {
    public class CloudApiContext : DbContext {

        public CloudApiContext(DbContextOptions<CloudApiContext> options) : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

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
                    .IsUnique(false)
                    .HasAnnotation("IndexSortOrder", "Descending");
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
                //Nvr信息
                modelBuilder.Entity<PackageInfoModel>()
                    .HasMany(b => b.NvrInfos)
                    .WithOne(n => n.PackageInfo)
                    .HasForeignKey(n => new { n.PackageId })
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<NvrInfoModel>().HasKey(c => new {
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

                //客户端配置
                modelBuilder.Entity<ExceptionTypeInfoModel>()
                    .HasKey(c => new {
                        c.Id
                    });
                modelBuilder.Entity<ExceptionTypeInfoModel>()
                    .HasMany(b => b.ExceptionMatchInfos)
                    .WithOne(n => n.ExceptionInfo)
                    .HasForeignKey(n => new { n.ExceptionTypeId })
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<ExceptionMatchInfoModel>().HasKey(c => new {
                    c.Id
                });


                //复用的本地配置

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
}