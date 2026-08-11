using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JayTom.Dws.Infrastructure.Migrations;

/// <summary>
/// 登记标识符和定点数 CLR 语义升级；SQLite 的表、列、类型及历史数据均保持不变。
/// </summary>
[DbContext(typeof(SqliteContext))]
[Migration("202608110001_FixedPointCompatibility")]
public sealed class FixedPointCompatibility : Migration {
    /// <summary>不执行结构变更，继续兼容既有 SQLite 文件。</summary>
    protected override void Up(MigrationBuilder migrationBuilder) {
    }

    /// <summary>不执行结构回滚，因为本迁移没有修改数据库结构。</summary>
    protected override void Down(MigrationBuilder migrationBuilder) {
    }
}
