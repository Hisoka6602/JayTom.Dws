using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JayTom.Dws.Infrastructure.Migrations;

/// <summary>登记后续模型语义清理，保持既有 SQLite 物理结构不变。</summary>
[DbContext(typeof(SqliteContext))]
[Migration("202608110002_ModelSemanticsCompatibility")]
public sealed class ModelSemanticsCompatibility : Migration {
    /// <summary>不修改业务表、列、索引或历史数据。</summary>
    protected override void Up(MigrationBuilder migrationBuilder) {
    }

    /// <summary>不执行结构回滚，因为本迁移没有物理变更。</summary>
    protected override void Down(MigrationBuilder migrationBuilder) {
    }
}
