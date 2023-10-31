using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace JayTom.Dws.Infrastructure {

    public sealed class SqliteLogsContext : DbContext {

        public SqliteLogsContext(DbContextOptions<SqliteLogsContext> options) : base(options) {
            lock (System.AppDomain.CurrentDomain.BaseDirectory) {
                var s = $"{System.AppDomain.CurrentDomain.BaseDirectory}ClientLogs.db";
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