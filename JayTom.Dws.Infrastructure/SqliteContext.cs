using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalData;
using JayTom.Dws.Data.LocalConf;
using Microsoft.EntityFrameworkCore;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

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

                modelBuilder.Entity<BarCodeInfoModel>()
                    .HasMany(b => b.PanoramaImagePaths)
                    .WithOne(n => n.BarcodeInfo)
                    .HasForeignKey(n => new { n.BarcodeInfoId })
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<PanoramaImageInfoModel>().HasKey(c => new {
                    c.Id
                });

                modelBuilder.Entity<SoundInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<SoundInfoModel>()
                    .HasIndex(b => b.SoundName)
                    .IsUnique();
            }
            //conf
            {
                //ConfigInfoModel
                modelBuilder.Entity<ConfigInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<ConfigInfoModel>()
                    .HasIndex(b => b.ConfigName)
                    .IsUnique();
                //BarcodeScannerCamera
                modelBuilder.Entity<BarcodeScannerCameraConfigInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<BarcodeScannerCameraConfigInfoModel>()
                    .HasIndex(b => b.SerialNumber)
                    .IsUnique();
                //PanoramaCamera
                modelBuilder.Entity<PanoramaCameraConfigInfoModel>().HasKey(c => new {
                    c.Id
                });

                modelBuilder.Entity<PanoramaCameraConfigInfoModel>()
                    .HasIndex(b => b.SerialNumber)
                    .IsUnique();
                //VolumeCamera
                modelBuilder.Entity<VolumeCameraConfigInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<VolumeCameraConfigInfoModel>()
                    .HasIndex(b => b.SerialNumber)
                    .IsUnique();
                //分拣
                modelBuilder.Entity<LogisticsCodeRecognitionInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<LogisticsCodeRecognitionInfoModel>()
                    .HasIndex(b => b.LogisticsCode)
                    .IsUnique();

                modelBuilder.Entity<PackageExitDefinitionInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<PackageExitDefinitionInfoModel>()
                    .HasIndex(b => new { b.Id, b.IsActive })
                    .IsUnique();
                modelBuilder.Entity<SortingInstructionBindingInfoModel>().HasKey(c => new {
                    c.Id
                });
                /*modelBuilder.Entity<SortingInstructionBindingInfoModel>()
                    .HasIndex(b => new { b.ExitId, b.IsActive })
                    .IsUnique();*/
                modelBuilder.Entity<SortingInstructionInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<SortingInstructionBindingInfoModel>()
                    .HasMany(b => b.InstructionItems)
                    .WithOne(n => n.SortingInstructionBindingInfo)
                    .HasForeignKey(n => new { n.InstructionBindingId })
                    .OnDelete(DeleteBehavior.Cascade);

                modelBuilder.Entity<LogisticsCodeRecognitionInfoModel>()
                    .HasMany(b => b.LogisticsRegexItems)
                    .WithOne(n => n.LogisticsCodeInfo)
                    .HasForeignKey(n => new { n.LogisticsId })
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<LogisticsRegexInfoModel>().HasKey(c => new {
                    c.Id
                });
            }
            //log
            {
                modelBuilder.Entity<InstructionLogInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<InstructionLogInfoModel>()
                    .HasIndex(b => b.TimestampedGuid)
                    .IsUnique(false);
                modelBuilder.Entity<InstructionLogInfoModel>()
                    .HasIndex(b => b.InstructionCreateTime)
                    .IsUnique(false)
                    .HasAnnotation("IndexSortOrder", "Descending");
            }

            base.OnModelCreating(modelBuilder);
        }
    }
}