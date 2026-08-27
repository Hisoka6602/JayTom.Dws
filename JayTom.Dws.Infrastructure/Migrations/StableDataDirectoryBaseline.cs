using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JayTom.Dws.Infrastructure.Migrations;

/// <summary>登记数据库迁入稳定用户数据目录后的持久化基线。</summary>
[DbContext(typeof(SqliteContext))]
[Migration("202608270001_StableDataDirectoryBaseline")]
internal sealed class StableDataDirectoryBaseline : Migration {
    /// <summary>数据位置迁移由启动协调器完成，物理表结构保持不变。</summary>
    protected override void Up(MigrationBuilder migrationBuilder) {
    }

    /// <summary>回退版本登记时不移动或删除任何用户数据库。</summary>
    protected override void Down(MigrationBuilder migrationBuilder) {
    }
}
