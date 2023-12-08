using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ConsoleApp5.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Data_VideoBarCodeInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimestampedGuid = table.Column<long>(type: "bigint", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScanTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_VideoBarCodeInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Data_VideoScanNodeInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarcodeId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScanTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_VideoScanNodeInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_VideoScanNodeInfo_Data_VideoBarCodeInfo_BarcodeId",
                        column: x => x.BarcodeId,
                        principalSchema: "dbo",
                        principalTable: "Data_VideoBarCodeInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Data_VideoNodeImageInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScanNodeId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageType = table.Column<int>(type: "int", nullable: false),
                    CameraSerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CameraName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_VideoNodeImageInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_VideoNodeImageInfo_Data_VideoScanNodeInfo_ScanNodeId",
                        column: x => x.ScanNodeId,
                        principalSchema: "dbo",
                        principalTable: "Data_VideoScanNodeInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Data_VideoNodeImageInfo_ScanNodeId",
                schema: "dbo",
                table: "Data_VideoNodeImageInfo",
                column: "ScanNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_VideoScanNodeInfo_BarcodeId",
                schema: "dbo",
                table: "Data_VideoScanNodeInfo",
                column: "BarcodeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Data_VideoNodeImageInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Data_VideoScanNodeInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Data_VideoBarCodeInfo",
                schema: "dbo");
        }
    }
}
