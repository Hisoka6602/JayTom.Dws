using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JayTom.Dws.VideoApiDbTest.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Conf_BarcodeScannerCameraConfigInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IsShowRealTimeImage = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CameraConnectionParameters = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsOcrSupported = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SerialNumber = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Model = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IpAddress = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CameraType = table.Column<int>(type: "int", nullable: false),
                    ConnectionType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conf_BarcodeScannerCameraConfigInfo", x => x.Id);
                })
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
                name: "Conf_NvrCameraBindingInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IpAddress = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Port = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Password = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    ScannerCameraConfigInfoModelId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conf_NvrCameraBindingInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conf_NvrCameraBindingInfo_Conf_BarcodeScannerCameraConfigInf~",
                        column: x => x.Id,
                        principalSchema: "dbo",
                        principalTable: "Conf_BarcodeScannerCameraConfigInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateTable(
                name: "Data_VideoNvrCameraBindingInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ScanNodeId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_VideoNvrCameraBindingInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_VideoNvrCameraBindingInfo_Conf_NvrCameraBindingInfo_Id",
                        column: x => x.Id,
                        principalSchema: "dbo",
                        principalTable: "Conf_NvrCameraBindingInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Data_VideoNvrCameraBindingInfo_Data_VideoScanNodeInfo_ScanNo~",
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
                name: "IX_Data_VideoNvrCameraBindingInfo_ScanNodeId",
                schema: "dbo",
                table: "Data_VideoNvrCameraBindingInfo",
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
                name: "Data_VideoNvrCameraBindingInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Conf_NvrCameraBindingInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Data_VideoScanNodeInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Conf_BarcodeScannerCameraConfigInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Data_VideoBarCodeInfo",
                schema: "dbo");
        }
    }
}
