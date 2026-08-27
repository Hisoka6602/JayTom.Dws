using JayTom.Dws.Models.LocalConf;
using JayTom.Dws.Models.ServerData;
using Microsoft.EntityFrameworkCore;

namespace JayTom.Dws.Infrastructure {

    public class SqlContext : DbContext {

        public SqlContext(DbContextOptions<SqlContext> options) : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            //创建表
            //多连接表
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<UserInfo>().HasKey(c => new {
                c.Id
            });
            modelBuilder.Entity<AuthorizationInfo>().HasKey(c => new {
                c.Id
            });
            // 配置关系
            modelBuilder.Entity<UserInfo>()
                .HasMany(u => u.AuthorizationInfos)
                .WithOne(a => a.UserInfo)
                .HasForeignKey(a => a.UserId);

            modelBuilder.Entity<MachineInfo>().HasKey(c => new {
                c.Id
            });
            modelBuilder.Entity<AuthorizationInfo>()
                .HasMany(a => a.MachineInfos)
                .WithOne(m => m.AuthorizationInfo)
                .HasForeignKey(m => m.AuthorizationInfoUserId);
        }
    }
}