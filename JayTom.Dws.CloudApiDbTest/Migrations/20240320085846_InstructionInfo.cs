using Microsoft.EntityFrameworkCore.Migrations;

namespace JayTom.Dws.CloudApiDbTest.Migrations
{
    public partial class InstructionInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Data_DeviceInfo_Data_PackageInfo_Id",
                schema: "dbo",
                table: "Data_DeviceInfo");

            migrationBuilder.CreateIndex(
                name: "IX_Data_DeviceInfo_PackageId",
                schema: "dbo",
                table: "Data_DeviceInfo",
                column: "PackageId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Data_DeviceInfo_Data_PackageInfo_PackageId",
                schema: "dbo",
                table: "Data_DeviceInfo",
                column: "PackageId",
                principalSchema: "dbo",
                principalTable: "Data_PackageInfo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Data_DeviceInfo_Data_PackageInfo_PackageId",
                schema: "dbo",
                table: "Data_DeviceInfo");

            migrationBuilder.DropIndex(
                name: "IX_Data_DeviceInfo_PackageId",
                schema: "dbo",
                table: "Data_DeviceInfo");

            migrationBuilder.AddForeignKey(
                name: "FK_Data_DeviceInfo_Data_PackageInfo_Id",
                schema: "dbo",
                table: "Data_DeviceInfo",
                column: "Id",
                principalSchema: "dbo",
                principalTable: "Data_PackageInfo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
