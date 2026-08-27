using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JayTom.Dws.Infrastructure.Migrations;

/// <summary>登记包裹映射迁入独立 Fluent Configuration，不改变既有物理结构。</summary>
[DbContext(typeof(SqliteContext))]
[Migration("202608140001_PackageModuleMappingBaseline")]
public sealed class PackageModuleMappingBaseline : Migration
{
    /// <summary>映射所有权变化不需要修改表、列或历史数据。</summary>
    protected override void Up(MigrationBuilder migrationBuilder) { }

    /// <summary>回退映射所有权不需要修改物理结构。</summary>
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
