using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalData;
using JayTom.Dws.Data.LocalConf;
using Microsoft.EntityFrameworkCore;

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
                modelBuilder.Entity<BarCodeInfoModel>().HasKey(c => new {
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
                //RequestParametersInfoModel
                modelBuilder.Entity<RequestParametersInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<RequestParametersInfoModel>()
                    .HasIndex(b => b.InterfaceName)
                    .IsUnique();
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