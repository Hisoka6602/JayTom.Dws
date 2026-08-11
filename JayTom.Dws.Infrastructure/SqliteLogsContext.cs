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
            SqliteDatabaseInitializer.EnsureInitialized(
                this, SqliteDatabaseInitializer.ResolveDatabasePath(this, "ClientLogs.db"));
        }

        /// <summary>保持既有 SQLite REAL 列结构，同时在业务模型中使用定点数。</summary>
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
            configurationBuilder.Properties<decimal>()
                .HaveColumnType("REAL");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            //log
            {
                //程序运行日志
                modelBuilder.Entity<AppLogInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<AppLogInfoModel>()
                    .HasIndex(b => b.CreateTime)
                    .IsUnique(false)
                    .HasAnnotation("IndexSortOrder", "Descending");
                //相机日志

                modelBuilder.Entity<CameraLogInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<CameraLogInfoModel>()
                    .HasIndex(b => b.CreateTime)
                    .IsUnique(false)
                    .HasAnnotation("IndexSortOrder", "Descending");
                //分拣日志
                modelBuilder.Entity<SortingLogInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<SortingLogInfoModel>()
                    .HasIndex(b => b.CreateTime)
                    .IsUnique(false)
                    .HasAnnotation("IndexSortOrder", "Descending");
                //称重日志
                modelBuilder.Entity<WeighingLogInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<WeighingLogInfoModel>()
                    .HasIndex(b => b.CreateTime)
                    .IsUnique(false)
                    .HasAnnotation("IndexSortOrder", "Descending");
                //体积日志
                modelBuilder.Entity<VolumeLogInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<VolumeLogInfoModel>()
                    .HasIndex(b => b.CreateTime)
                    .IsUnique(false)
                    .HasAnnotation("IndexSortOrder", "Descending");
                //API日志
                modelBuilder.Entity<ApiLogInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<ApiLogInfoModel>()
                    .HasIndex(b => b.CreateTime)
                    .IsUnique(false)
                    .HasAnnotation("IndexSortOrder", "Descending");
                //输出日志
                modelBuilder.Entity<OutputLogInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<OutputLogInfoModel>()
                    .HasIndex(b => b.CreateTime)
                    .IsUnique(false)
                    .HasAnnotation("IndexSortOrder", "Descending");
                //输入日志
                modelBuilder.Entity<InputLogInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<InputLogInfoModel>()
                    .HasIndex(b => b.CreateTime)
                    .IsUnique(false)
                    .HasAnnotation("IndexSortOrder", "Descending");
                //OCR日志
                modelBuilder.Entity<OcrLogInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<OcrLogInfoModel>()
                    .HasIndex(b => b.CreateTime)
                    .IsUnique(false)
                    .HasAnnotation("IndexSortOrder", "Descending");
                //FTP日志
                modelBuilder.Entity<FtpLogInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<FtpLogInfoModel>()
                    .HasIndex(b => b.CreateTime)
                    .IsUnique(false)
                    .HasAnnotation("IndexSortOrder", "Descending");
                //清理记录
                modelBuilder.Entity<LogCleaningLogInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<LogCleaningLogInfoModel>()
                    .HasIndex(b => b.CreateTime)
                    .IsUnique(false)
                    .HasAnnotation("IndexSortOrder", "Descending");
                //异常日志
                modelBuilder.Entity<ExceptionLogInfoModel>().HasKey(c => new {
                    c.Id
                });
                modelBuilder.Entity<ExceptionLogInfoModel>()
                    .HasIndex(b => b.CreateTime)
                    .IsUnique(false)
                    .HasAnnotation("IndexSortOrder", "Descending");
            }
            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            base.OnConfiguring(optionsBuilder);
            //optionsBuilder.EnableSensitiveDataLogging(); // 启用敏感数据日志
        }
    }
}
