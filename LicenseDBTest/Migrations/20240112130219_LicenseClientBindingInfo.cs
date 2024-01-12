using Microsoft.EntityFrameworkCore.Migrations;

namespace LicenseDBTest.Migrations
{
    public partial class LicenseClientBindingInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LicenseCode",
                schema: "dbo",
                table: "Code_LicenseClientBindingInfo");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LicenseCode",
                schema: "dbo",
                table: "Code_LicenseClientBindingInfo",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
