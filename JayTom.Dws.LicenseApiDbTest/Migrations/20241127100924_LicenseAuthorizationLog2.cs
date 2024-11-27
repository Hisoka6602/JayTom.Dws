using Microsoft.EntityFrameworkCore.Migrations;

namespace JayTom.Dws.LicenseApiDbTest.Migrations
{
    public partial class LicenseAuthorizationLog2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tenant",
                schema: "dbo",
                table: "Log_LicenseAuthorizationLog");

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                schema: "dbo",
                table: "Log_LicenseAuthorizationLog",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Log_LicenseAuthorizationLog_Sys_LicenseUserInfo_Id",
                schema: "dbo",
                table: "Log_LicenseAuthorizationLog",
                column: "Id",
                principalSchema: "dbo",
                principalTable: "Sys_LicenseUserInfo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Log_LicenseAuthorizationLog_Sys_LicenseUserInfo_Id",
                schema: "dbo",
                table: "Log_LicenseAuthorizationLog");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "dbo",
                table: "Log_LicenseAuthorizationLog");

            migrationBuilder.AddColumn<string>(
                name: "Tenant",
                schema: "dbo",
                table: "Log_LicenseAuthorizationLog",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
