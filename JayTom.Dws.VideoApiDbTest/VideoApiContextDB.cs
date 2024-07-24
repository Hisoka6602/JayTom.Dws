using JayTom.Dws.Data.VideoApiData;
using Microsoft.EntityFrameworkCore;
using JayTom.Dws.Data.LocalConf.CloudConfig;

namespace JayTom.Dws.VideoApiDbTest {

    public class VideoApiContextDB : DbContext {

        public VideoApiContextDB(DbContextOptions<VideoApiContextDB> options) : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            // 配置基类的主键
            modelBuilder.Entity<NvrCameraBindingInfoModel>()
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
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}