using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ConsoleApp5.Migrations
{
    public partial class MysqlMigrationName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Data_VideoBarCodeInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TimestampedGuid = table.Column<long>(type: "bigint", nullable: false),
                    Barcode = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ScanTime = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_VideoBarCodeInfo", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Data_VideoScanNodeInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BarcodeId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ScanTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Data_VideoNodeImageInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ScanNodeId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Path = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImageType = table.Column<int>(type: "int", nullable: false),
                    CameraSerialNumber = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CameraName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
