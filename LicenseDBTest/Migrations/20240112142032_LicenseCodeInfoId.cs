using Microsoft.EntityFrameworkCore.Migrations;

namespace LicenseDBTest.Migrations
{
    public partial class LicenseCodeInfoId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Code_LicenseCodeInfo_Sys_LicenseUserInfo_Id",
                schema: "dbo",
                table: "Code_LicenseCodeInfo");

            migrationBuilder.CreateIndex(
                name: "IX_Code_LicenseCodeInfo_UserId",
                schema: "dbo",
                table: "Code_LicenseCodeInfo",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Code_LicenseCodeInfo_Sys_LicenseUserInfo_UserId",
                schema: "dbo",
                table: "Code_LicenseCodeInfo",
                column: "UserId",
                principalSchema: "dbo",
                principalTable: "Sys_LicenseUserInfo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Code_LicenseCodeInfo_Sys_LicenseUserInfo_UserId",
                schema: "dbo",
                table: "Code_LicenseCodeInfo");

            migrationBuilder.DropIndex(
                name: "IX_Code_LicenseCodeInfo_UserId",
                schema: "dbo",
                table: "Code_LicenseCodeInfo");

            migrationBuilder.AddForeignKey(
                name: "FK_Code_LicenseCodeInfo_Sys_LicenseUserInfo_Id",
                schema: "dbo",
                table: "Code_LicenseCodeInfo",
                column: "Id",
                principalSchema: "dbo",
                principalTable: "Sys_LicenseUserInfo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
