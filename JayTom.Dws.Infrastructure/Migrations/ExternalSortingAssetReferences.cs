using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JayTom.Dws.Infrastructure.Migrations;

/// <summary>为物流识别配置增加外部声音和图标资源引用。</summary>
[DbContext(typeof(SqliteConfContext))]
[Migration("202608140004_ExternalSortingAssetReferences")]
public sealed class ExternalSortingAssetReferences : Migration {
    /// <summary>增加外部资源引用列；旧二进制列仅保留供升级导出。</summary>
    protected override void Up(MigrationBuilder migrationBuilder) {
    }

    /// <summary>回退外部资源引用列。</summary>
    protected override void Down(MigrationBuilder migrationBuilder) {
    }
}
