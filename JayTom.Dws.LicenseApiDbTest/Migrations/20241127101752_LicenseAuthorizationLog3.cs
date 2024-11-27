using Microsoft.EntityFrameworkCore.Migrations;

namespace JayTom.Dws.LicenseApiDbTest.Migrations
{
    public partial class LicenseAuthorizationLog3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Log_LicenseAuthorizationLog_Sys_LicenseUserInfo_Id",
                schema: "dbo",
                table: "Log_LicenseAuthorizationLog");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
