using Microsoft.EntityFrameworkCore;

namespace JayTom.Dws.Infrastructure {

    public class SqlContext : DbContext {

        public SqlContext(DbContextOptions<SqlContext> options) : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
        }
    }
}