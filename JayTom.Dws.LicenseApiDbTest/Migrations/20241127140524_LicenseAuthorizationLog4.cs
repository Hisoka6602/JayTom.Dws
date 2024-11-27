using Microsoft.EntityFrameworkCore.Migrations;

namespace JayTom.Dws.LicenseApiDbTest.Migrations
{
    public partial class LicenseAuthorizationLog4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "dbo",
                table: "Log_LicenseAuthorizationLog");

            migrationBuilder.AddColumn<string>(
                name: "UserCode",
                schema: "dbo",
                table: "Log_LicenseAuthorizationLog",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserCode",
                schema: "dbo",
                table: "Log_LicenseAuthorizationLog");

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                schema: "dbo",
                table: "Log_LicenseAuthorizationLog",
                type: "bigint",
                nullable: true);
        }
    }
}
