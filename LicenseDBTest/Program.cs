using System.Reflection.Emit;
using JayTom.Dws.Data.License;
using JayTom.Dws.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

internal class Program {

    private static void Main(string[] args) {
        CreateMigration();
        Console.WriteLine("Migration completed successfully.");
        Console.ReadLine();
        Console.WriteLine("Hello, World!");
    }

    public static void CreateMigration() {
        var dbContextFactory = new DesignTimeDbContextFactory();
        using (var context = dbContextFactory.CreateDbContext(null)) {
            context.Database.Migrate();
        }
    }

    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LicenseApiContext1> {

        public LicenseApiContext1 CreateDbContext(string[] args) {
            var optionsBuilder = new DbContextOptionsBuilder<LicenseApiContext1>();

            optionsBuilder.UseMySql("Server=localhost;Port=3306;Password=wWfenVYJxN1Iy0FB;Database=License;User=root;",
                ServerVersion.AutoDetect("Server=localhost;Port=3306;Password=wWfenVYJxN1Iy0FB;Database=License;User=root;"),
            builder => {
                builder.SchemaBehavior(MySqlSchemaBehavior.Ignore);
            });
            return new LicenseApiContext1(optionsBuilder.Options);
        }
    }

    public class LicenseApiContext1 : DbContext {

        public LicenseApiContext1(DbContextOptions<LicenseApiContext1> options) : base(options) {
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

            /*modelBuilder.Entity<LicensePermissionTemplateInfo>()
                .HasOne(b => b.LicenseApplicationInfo)
                .WithMany()
                .HasForeignKey(b => b.LicenseApplicationInfoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LicenseApplicationInfo>()
                .HasOne(b => b.LicensePermissionTemplate)
                .WithMany()
                .HasForeignKey(b => b.LicensePermissionTemplateId);*/
            modelBuilder.Entity<LicensePermissionTemplateInfo>()
                .HasOne(b => b.LicenseApplicationInfo)
                .WithOne(n => n.LicensePermissionTemplate)
                .HasForeignKey<LicenseApplicationInfo>(b => b.LicensePermissionTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LicenseCodeInfo>()
                .HasMany(b => b.LicenseClientBindingInfo)
                .WithOne(n => n.LicenseCodeInfo)
                .HasForeignKey(n => new { n.LicenseCodeId })
                .OnDelete(DeleteBehavior.Cascade);

            /*
            modelBuilder.Entity<LicensePermissionTemplateInfo>()
                .HasMany(b => b.LicenseCodeInfos)
                .WithOne(n => n.LicensePermissionTemplateInfo)
                .HasForeignKey(n => new { n.LicensePermissionTemplateInfoId })
                .OnDelete(DeleteBehavior.Cascade);*/
            modelBuilder.Entity<LicenseCodeInfo>()
                .HasOne(n => n.LicensePermissionTemplateInfo)
                .WithMany(b => b.LicenseCodeInfos)
                .HasForeignKey(n => n.LicensePermissionTemplateInfoId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<LicenseCodeInfo>()
                .HasOne(n => n.UserInfo)
                .WithMany(b => b.LicenseCodeInfos)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}