using Microsoft.EntityFrameworkCore.Migrations;

namespace JayTom.Dws.VideoApiDbTest.Migrations
{
    public partial class AddIndexesAndNewFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Data_BarCodeInfo_Barcode",
                schema: "dbo",
                table: "Data_BarCodeInfo");

            migrationBuilder.DropIndex(
                name: "IX_Data_BarCodeInfo_ScanTime",
                schema: "dbo",
                table: "Data_BarCodeInfo");

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                schema: "dbo",
                table: "Data_BarCodeInfo",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                schema: "dbo",
                table: "Data_BarCodeInfo",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Data_BarCodeInfo_Barcode",
                schema: "dbo",
                table: "Data_BarCodeInfo",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_Data_BarCodeInfo_ScanTime",
                schema: "dbo",
                table: "Data_BarCodeInfo",
                column: "ScanTime");
        }
    }
}
