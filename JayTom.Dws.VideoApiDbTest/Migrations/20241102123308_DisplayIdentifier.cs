using Microsoft.EntityFrameworkCore.Migrations;

namespace JayTom.Dws.VideoApiDbTest.Migrations
{
    public partial class DisplayIdentifier : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CameraSerialNumber",
                schema: "dbo",
                table: "Data_BarCodeInfo",
                newName: "SerialNumber");

            migrationBuilder.AddColumn<string>(
                name: "DisplayIdentifier",
                schema: "dbo",
                table: "Data_BarCodeInfo",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayIdentifier",
                schema: "dbo",
                table: "Data_BarCodeInfo");

            migrationBuilder.RenameColumn(
                name: "SerialNumber",
                schema: "dbo",
                table: "Data_BarCodeInfo",
                newName: "CameraSerialNumber");
        }
    }
}
