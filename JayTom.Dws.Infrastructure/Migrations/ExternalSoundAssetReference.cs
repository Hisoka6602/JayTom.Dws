using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JayTom.Dws.Infrastructure.Migrations;

/// <summary>为声音实体增加外部资源引用，停止向数据库写入文件内容。</summary>
[DbContext(typeof(SqliteContext))]
[Migration("202608140003_ExternalSoundAssetReference")]
public sealed class ExternalSoundAssetReference : Migration {
    /// <summary>增加外部资源引用列；旧二进制列仅保留供升级导出。</summary>
    protected override void Up(MigrationBuilder migrationBuilder) {
    }

    /// <summary>回退外部资源引用列。</summary>
    protected override void Down(MigrationBuilder migrationBuilder) {
    }
}
