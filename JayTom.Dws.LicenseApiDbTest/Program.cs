using JayTom.Dws.Data.License;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Infrastructure;
using JayTom.Dws.Data.CloudApiData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

internal class Program {

    private static void Main(string[] args) {
        var migrationSuccess = CreateMigration();
        if (migrationSuccess) {
            Console.WriteLine("Migration completed successfully.");
            SetIndexesToDescending();
        }
        else {
            Console.WriteLine("Migration failed. Check logs for details.");
        }

        Console.ReadLine();
    }

    public static void SetIndexesToDescending() {
    }

    public static bool CreateMigration() {
        var dbContextFactory = new DesignTimeDbContextFactory();
        try {
            using var context = dbContextFactory.CreateDbContext(null);
            context.Database.Migrate();
            return true; // Migration succeeded
        }
        catch (Exception ex) {
            Console.WriteLine($"Migration failed: {ex.Message}");
            return false; // Migration failed
        }
    }

    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LicenseApiContext1> {

        public LicenseApiContext1 CreateDbContext(string[] args) {
            /*var optionsBuilder = new DbContextOptionsBuilder<CloudApiContext1>();
            //f6vQDiiWpXLDUCxR
            optionsBuilder.UseMySql("Server=localhost;Port=3306;Password=f6vQDiiWpXLDUCxR;Database=CloudApi;User=root;",
                ServerVersion.AutoDetect("Server=localhost;Port=3306;Password=f6vQDiiWpXLDUCxR;Database=CloudApi;User=root;"),
            builder => {
                builder.SchemaBehavior(MySqlSchemaBehavior.Ignore);
            });
            return new CloudApiContext1(optionsBuilder.Options);*/
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

            var optionsBuilder = new DbContextOptionsBuilder<LicenseApiContext1>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), builder => {
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
            modelBuilder.Entity<LicenseAppLicenseInfo>().HasKey(c => new {
                c.Id
            });
            modelBuilder.Entity<LicenseGroupInfo>().HasKey(c => new {
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
                .HasForeignKey<LicenseApplicationInfo>(b => b.LicensePermissionTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            /*modelBuilder.Entity<LicensePermissionTemplateInfo>()
                .HasOne(b => b.LicenseApplicationInfo)
                .WithOne(n => n.LicensePermissionTemplate)
                .HasForeignKey<LicenseApplicationInfo>(n => n.LicensePermissionTemplateId);
                */

            modelBuilder.Entity<LicenseCodeInfo>()
                .HasMany(b => b.LicenseClientBindingInfo)
                .WithOne(n => n.LicenseCodeInfo)
                .HasForeignKey(n => new { n.LicenseCodeId })
                .OnDelete(DeleteBehavior.Cascade);

            /*modelBuilder.Entity<LicensePermissionTemplateInfo>()
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

            //----------------------
            modelBuilder.Entity<LicenseAppLicenseInfo>()
                .HasOne(n => n.LicensePermissionTemplateInfo)
                .WithMany(b => b.AppLicenseInfos)
                .HasForeignKey(n => n.LicensePermissionTemplateInfoId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<LicenseAppLicenseInfo>()
                .HasOne(n => n.UserInfo)
                .WithMany(b => b.AppLicenseInfos)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            //----------------------
            modelBuilder.Entity<LicenseGroupInfo>()
                .HasMany(b => b.LicenseCodeInfos)
                .WithOne(n => n.LicenseGroupInfo)
                .HasForeignKey(n => new { n.LicenseGroupInfoId })
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}