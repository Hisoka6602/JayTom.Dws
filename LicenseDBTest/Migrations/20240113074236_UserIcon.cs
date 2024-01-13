using Microsoft.EntityFrameworkCore.Migrations;

namespace LicenseDBTest.Migrations
{
    public partial class UserIcon : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserIcon",
                schema: "dbo",
                table: "Sys_LicenseUserInfo",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserIcon",
                schema: "dbo",
                table: "Sys_LicenseUserInfo");
        }
    }
}
