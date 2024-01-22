using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JayTom.Dws.CloudApiDbTest.Migrations
{
    public partial class Migrations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Data_PackageInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PackageTimestamped = table.Column<long>(type: "bigint", nullable: false),
                    PackageCreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Other = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_PackageInfo", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Data_BarCodeInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Barcode = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ScanTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    CameraSerialNumber = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PackageId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_BarCodeInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_BarCodeInfo_Data_PackageInfo_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "dbo",
                        principalTable: "Data_PackageInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Data_CloudVideoUploadInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UploadTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UploadContent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponseContent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UploadDuration = table.Column<int>(type: "int", nullable: true),
                    TargetAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ScanImageCount = table.Column<int>(type: "int", nullable: false),
                    PanoramaImageCount = table.Column<int>(type: "int", nullable: false),
                    PackageId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_CloudVideoUploadInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_CloudVideoUploadInfo_Data_PackageInfo_Id",
                        column: x => x.Id,
                        principalSchema: "dbo",
                        principalTable: "Data_PackageInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Data_DeviceInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MachineCode = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NodeName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PackageId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_DeviceInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_DeviceInfo_Data_PackageInfo_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "dbo",
                        principalTable: "Data_PackageInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Data_ExitInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TheoreticalExit = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhysicalExit = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhysicalExitId = table.Column<long>(type: "bigint", nullable: false),
                    PackageId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_ExitInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_ExitInfo_Data_PackageInfo_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "dbo",
                        principalTable: "Data_PackageInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Data_ImageInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CameraName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomCameraName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CameraSerialNumber = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    LocalPath = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImageUrl = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PackageId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_ImageInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_ImageInfo_Data_PackageInfo_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "dbo",
                        principalTable: "Data_PackageInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Data_LogisticsInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LogisticsCode = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LogisticsName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PackageId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_LogisticsInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_LogisticsInfo_Data_PackageInfo_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "dbo",
                        principalTable: "Data_PackageInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Data_OcrInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OriginalContent = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsUseOcr = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ThreeSegmentCode = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ElapsedMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    RecognizeTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    VirtualNumberLast4 = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CameraSerialNumber = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubmitTimestamp = table.Column<long>(type: "bigint", nullable: false),
                    PackageId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_OcrInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_OcrInfo_Data_PackageInfo_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "dbo",
                        principalTable: "Data_PackageInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Data_SortingInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IsSortingUsed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SortingMode = table.Column<int>(type: "int", nullable: false),
                    SentInstruction = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SendTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReceivedInstruction = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReceivedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PackageCreationInstruction = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsCreatedByLowerMachine = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CommandTarget = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommunicationMethod = table.Column<int>(type: "int", nullable: false),
                    ChecksumProtocolName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConnectionName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsAbnormalSorting = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PackageId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_SortingInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_SortingInfo_Data_PackageInfo_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "dbo",
                        principalTable: "Data_PackageInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Data_UploadInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RequestStatus = table.Column<int>(type: "int", nullable: false),
                    RequestContent = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponseContent = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ResponseTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DurationInSeconds = table.Column<double>(type: "double", nullable: false),
                    InterfaceParameters = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestUrl = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExceptionMessage = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApiExceptionType = table.Column<int>(type: "int", nullable: false),
                    PackageId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_UploadInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_UploadInfo_Data_PackageInfo_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "dbo",
                        principalTable: "Data_PackageInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Data_VolumeInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    OriginalText = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FormattedLength = table.Column<double>(type: "double", nullable: false),
                    FormattedWidth = table.Column<double>(type: "double", nullable: false),
                    FormattedHeight = table.Column<double>(type: "double", nullable: false),
                    FormattedVolume = table.Column<double>(type: "double", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PackageId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_VolumeInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_VolumeInfo_Data_PackageInfo_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "dbo",
                        principalTable: "Data_PackageInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Data_WeightInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    OriginalText = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FormattedWeight = table.Column<double>(type: "double", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    WeighingMode = table.Column<int>(type: "int", nullable: false),
                    PackageId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_WeightInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_WeightInfo_Data_PackageInfo_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "dbo",
                        principalTable: "Data_PackageInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OcrDetailedInfoModel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OcrInfoId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Address = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InfoType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcrDetailedInfoModel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OcrDetailedInfoModel_Data_OcrInfo_OcrInfoId",
                        column: x => x.OcrInfoId,
                        principalSchema: "dbo",
                        principalTable: "Data_OcrInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Data_BarCodeInfo_Barcode",
                schema: "dbo",
                table: "Data_BarCodeInfo",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_Data_BarCodeInfo_PackageId",
                schema: "dbo",
                table: "Data_BarCodeInfo",
                column: "PackageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Data_BarCodeInfo_ScanTime",
                schema: "dbo",
                table: "Data_BarCodeInfo",
                column: "ScanTime");

            migrationBuilder.CreateIndex(
                name: "IX_Data_DeviceInfo_PackageId",
                schema: "dbo",
                table: "Data_DeviceInfo",
                column: "PackageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Data_ExitInfo_PackageId",
                schema: "dbo",
                table: "Data_ExitInfo",
                column: "PackageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Data_ImageInfo_PackageId",
                schema: "dbo",
                table: "Data_ImageInfo",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_LogisticsInfo_PackageId",
                schema: "dbo",
                table: "Data_LogisticsInfo",
                column: "PackageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Data_OcrInfo_PackageId",
                schema: "dbo",
                table: "Data_OcrInfo",
                column: "PackageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Data_PackageInfo_PackageCreateTime",
                schema: "dbo",
                table: "Data_PackageInfo",
                column: "PackageCreateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Data_PackageInfo_PackageTimestamped",
                schema: "dbo",
                table: "Data_PackageInfo",
                column: "PackageTimestamped");

            migrationBuilder.CreateIndex(
                name: "IX_Data_SortingInfo_PackageId",
                schema: "dbo",
                table: "Data_SortingInfo",
                column: "PackageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Data_UploadInfo_PackageId",
                schema: "dbo",
                table: "Data_UploadInfo",
                column: "PackageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Data_VolumeInfo_PackageId",
                schema: "dbo",
                table: "Data_VolumeInfo",
                column: "PackageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Data_WeightInfo_PackageId",
                schema: "dbo",
                table: "Data_WeightInfo",
                column: "PackageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OcrDetailedInfoModel_OcrInfoId",
                table: "OcrDetailedInfoModel",
                column: "OcrInfoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Data_BarCodeInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Data_CloudVideoUploadInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Data_DeviceInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Data_ExitInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Data_ImageInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Data_LogisticsInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Data_SortingInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Data_UploadInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Data_VolumeInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Data_WeightInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "OcrDetailedInfoModel");

            migrationBuilder.DropTable(
                name: "Data_OcrInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Data_PackageInfo",
                schema: "dbo");
        }
    }
}
