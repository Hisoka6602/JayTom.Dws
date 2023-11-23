using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.ConnectionParams;

namespace JayTom.Dws.Infrastructure {

    public sealed class SqliteConfContext : DbContext {

        public SqliteConfContext(DbContextOptions<SqliteConfContext> options) : base(options) {
            lock (System.AppDomain.CurrentDomain.BaseDirectory) {
                var s = $"{System.AppDomain.CurrentDomain.BaseDirectory}Configuration.db";
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
                modelBuilder.Entity<LogisticsCodeRecognitionInfoModel>()
                    .HasMany(b => b.LogisticsRegexItems)
                    .WithOne(n => n.LogisticsCodeInfo)
                    .HasForeignKey(n => new { n.LogisticsId })
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<LogisticsRegexInfoModel>().HasKey(c => new {
                    c.Id
                });
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

                modelBuilder.Entity<BarCodeSortingInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<BarCodeRegexInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<BarCodeSortingInfoModel>()
                    .HasMany(b => b.BarCodeRegexItems)
                    .WithOne(n => n.BarCodeSortingInfo)
                    .HasForeignKey(n => new { n.BarCodeSortingId })
                    .OnDelete(DeleteBehavior.Cascade);

                modelBuilder.Entity<WeightSortingInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<WeightRuleInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<WeightSortingInfoModel>()
                    .HasMany(b => b.WeightRuleItems)
                    .WithOne(n => n.WeightSortingInfo)
                    .HasForeignKey(n => new { n.WeightSortingId })
                    .OnDelete(DeleteBehavior.Cascade);

                modelBuilder.Entity<VolumeSortingInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<VolumeRuleInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<VolumeSortingInfoModel>()
                    .HasMany(b => b.VolumeRuleItems)
                    .WithOne(n => n.VolumeSortingInfo)
                    .HasForeignKey(n => new { n.VolumeSortingId })
                    .OnDelete(DeleteBehavior.Cascade);

                modelBuilder.Entity<LogisticsSortingInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<LogisticsRuleInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<LogisticsSortingInfoModel>()
                    .HasMany(b => b.LogisticsRuleItems)
                    .WithOne(n => n.LogisticsSortingInfo)
                    .HasForeignKey(n => new { n.LogisticsSortingId })
                    .OnDelete(DeleteBehavior.Cascade);

                modelBuilder.Entity<OcrSortingInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<OcrRuleInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<OcrSortingInfoModel>()
                    .HasMany(b => b.OcrRuleItems)
                    .WithOne(n => n.OcrSortingInfo)
                    .HasForeignKey(n => new { n.OcrSortingId })
                    .OnDelete(DeleteBehavior.Cascade);

                modelBuilder.Entity<ApiSortingInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<ApiRuleInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<ApiSortingInfoModel>()
                    .HasMany(b => b.ApiRuleItems)
                    .WithOne(n => n.ApiSortingInfo)
                    .HasForeignKey(n => new { n.ApiSortingId })
                    .OnDelete(DeleteBehavior.Cascade);

                //分拣相关
                //连接表
                modelBuilder.Entity<CommunicationConnectionConfigInfoModel>()
                    .HasKey(c => new {
                        c.Id
                    });
                //关联出口
                modelBuilder.Entity<CommunicationConnectionConfigInfoModel>()
                    .HasMany(b => b.PackageExitDefinitionItems)
                    .WithOne(n => n.CommunicationConnectionConfigInfo)
                    .HasForeignKey(n => new { n.CommunicationConnectionId })
                    .OnDelete(DeleteBehavior.Cascade);
                //下位机信息
                modelBuilder.Entity<CommunicationConnectionConfigInfoModel>()
                    .HasOne(b => b.DeviceExtensionConfigInfo)
                    .WithOne(n => n.CommunicationConnectionConfigInfo)
                    .HasForeignKey<DeviceExtensionConfigInfoModel>(n => n.CommunicationConnectionId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<DeviceExtensionConfigInfoModel>().HasKey(c => new {
                    c.Id
                });
                //心跳信息
                modelBuilder.Entity<CommunicationConnectionConfigInfoModel>()
                    .HasOne(b => b.HeartbeatConfigInfo)
                    .WithOne(n => n.CommunicationConnectionConfigInfo)
                    .HasForeignKey<HeartbeatConfigInfoModel>(n => n.CommunicationConnectionId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<HeartbeatConfigInfoModel>().HasKey(c => new {
                    c.Id
                });
                //串口信息
                modelBuilder.Entity<CommunicationConnectionConfigInfoModel>()
                    .HasOne(b => b.SerialPortConfigInfo)
                    .WithOne(n => n.CommunicationConnectionConfigInfo)
                    .HasForeignKey<SerialPortConfigInfoModel>(n => n.CommunicationConnectionId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<SerialPortConfigInfoModel>().HasKey(c => new {
                    c.Id
                });
                //Tcp信息连接
                modelBuilder.Entity<CommunicationConnectionConfigInfoModel>()
                    .HasOne(b => b.TcpConnectionConfigInfo)
                    .WithOne(n => n.CommunicationConnectionConfigInfo)
                    .HasForeignKey<TcpConnectionConfigInfoModel>(n => n.CommunicationConnectionId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<TcpConnectionConfigInfoModel>().HasKey(c => new {
                    c.Id
                });
                //Tcp连接配置
                //服务端
                modelBuilder.Entity<TcpConnectionConfigInfoModel>()
                    .HasOne(b => b.ServerParameter)
                    .WithOne()
                    .HasForeignKey<TcpConfigInfoModel>(n => n.TcpConnectionConfigId)
                    .OnDelete(DeleteBehavior.Cascade);
                //客户端
                modelBuilder.Entity<TcpConnectionConfigInfoModel>()
                    .HasOne(b => b.ClientParameter)
                    .WithOne()
                    .HasForeignKey<TcpConfigInfoModel>(n => n.TcpConnectionConfigId)
                    .OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<TcpConfigInfoModel>().HasKey(c => new {
                    c.Id
                });
            }
            base.OnModelCreating(modelBuilder);
        }
    }
}