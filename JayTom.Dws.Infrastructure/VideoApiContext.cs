using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.ServerData;
using JayTom.Dws.Data.VideoApiData;
using Microsoft.EntityFrameworkCore;

namespace JayTom.Dws.Infrastructure {

    public class VideoApiContext : DbContext {

        public VideoApiContext(DbContextOptions<VideoApiContext> options) : base(options) {
        }

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
            modelBuilder.Entity<VideoNvrCameraBindingInfoModel>().HasKey(c => new {
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

            modelBuilder.Entity<VideoScanNodeInfoModel>()
                .HasMany(b => b.VideoNvrCameraBindingInfos)
                .WithOne(n => n.ScanNodeInfo)
                .HasForeignKey(n => n.ScanNodeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}