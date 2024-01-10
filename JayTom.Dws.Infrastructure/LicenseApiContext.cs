using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Emit;
using JayTom.Dws.Data.License;
using System.Collections.Generic;
using JayTom.Dws.Data.VideoApiData;
using Microsoft.EntityFrameworkCore;

namespace JayTom.Dws.Infrastructure {

    public class LicenseApiContext : DbContext {

        public LicenseApiContext(DbContextOptions<LicenseApiContext> options) : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<LicenseApplicationInfo>().HasKey(c => new {
                c.Id
            });
            modelBuilder.Entity<LicenseClientBindingInfo>().HasKey(c => new {
                c.Id
            });
            modelBuilder.Entity<LicenseCodeInfo>().HasKey(c => new {
                c.Id
            });
            modelBuilder.Entity<LicenseFeatureInfo>().HasKey(c => new {
                c.Id
            });
            modelBuilder.Entity<LicensePermissionTemplateInfo>().HasKey(c => new {
                c.Id
            });
            modelBuilder.Entity<LicenseUserDetailsInfo>().HasKey(c => new {
                c.Id
            });
            modelBuilder.Entity<LicenseUserInfo>().HasKey(c => new {
                c.Id
            });
            //配置关系
            modelBuilder.Entity<LicenseUserInfo>()
                .HasOne(b => b.UserDetailsInfo)
                .WithOne(n => n.UserInfo)
                .HasForeignKey<LicenseUserDetailsInfo>(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LicenseApplicationInfo>()
                .HasMany(b => b.LicenseFeatureInfos)
                .WithOne(n => n.LicenseApplicationInfo)
                .HasForeignKey(n => new { n.LicenseApplicationInfoId })
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LicensePermissionTemplateInfo>()
                .HasOne(b => b.LicenseApplicationInfo)
                .WithOne(n => n.LicensePermissionTemplate)
                .HasForeignKey<LicenseApplicationInfo>(n => n.LicensePermissionTemplateId);

            modelBuilder.Entity<LicenseCodeInfo>()
                .HasMany(b => b.LicenseClientBindingInfo)
                .WithOne(n => n.LicenseCodeInfo)
                .HasForeignKey(n => new { n.LicenseCodeId })
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LicenseApplicationInfo>()
                .HasMany(b => b.LicenseCodeInfos)
                .WithOne(n => n.LicenseApplicationInfo)
                .HasForeignKey(n => new { n.LicenseApplicationInfoId })
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}