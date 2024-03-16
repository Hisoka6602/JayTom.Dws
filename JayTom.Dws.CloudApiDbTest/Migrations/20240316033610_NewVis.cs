using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JayTom.Dws.CloudApiDbTest.Migrations
{
    public partial class NewVis : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Data_DeviceInfo_Data_PackageInfo_PackageId",
                schema: "dbo",
                table: "Data_DeviceInfo");

            migrationBuilder.DropIndex(
                name: "IX_Data_DeviceInfo_PackageId",
                schema: "dbo",
                table: "Data_DeviceInfo");

            migrationBuilder.DropColumn(
                name: "CommandTarget",
                schema: "dbo",
                table: "Data_SortingInfo");

            migrationBuilder.DropColumn(
                name: "PackageCreationInstruction",
                schema: "dbo",
                table: "Data_SortingInfo");

            migrationBuilder.DropColumn(
                name: "ReceivedInstruction",
                schema: "dbo",
                table: "Data_SortingInfo");

            migrationBuilder.DropColumn(
                name: "ReceivedTime",
                schema: "dbo",
                table: "Data_SortingInfo");

            migrationBuilder.DropColumn(
                name: "SendTime",
                schema: "dbo",
                table: "Data_SortingInfo");

            migrationBuilder.RenameColumn(
                name: "SentInstruction",
                schema: "dbo",
                table: "Data_SortingInfo",
                newName: "SortingCode");

            migrationBuilder.CreateTable(
                name: "AggregatePackagesInfoModel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AggregatePackageCode = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PackagingTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PackageId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregatePackagesInfoModel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AggregatePackagesInfoModel_Data_PackageInfo_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "dbo",
                        principalTable: "Data_PackageInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Data_InstructionInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SortingInfoId = table.Column<long>(type: "bigint", nullable: false),
                    InstructionContent = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InstructionGeneratedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    InstructionType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_InstructionInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_InstructionInfo_Data_SortingInfo_SortingInfoId",
                        column: x => x.SortingInfoId,
                        principalSchema: "dbo",
                        principalTable: "Data_SortingInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AggregatePackagesInfoModel_PackageId",
                table: "AggregatePackagesInfoModel",
                column: "PackageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Data_InstructionInfo_SortingInfoId",
                schema: "dbo",
                table: "Data_InstructionInfo",
                column: "SortingInfoId");

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Data_DeviceInfo_Data_PackageInfo_Id",
                schema: "dbo",
                table: "Data_DeviceInfo");

            migrationBuilder.DropTable(
                name: "AggregatePackagesInfoModel");

            migrationBuilder.DropTable(
                name: "Data_InstructionInfo",
                schema: "dbo");

            migrationBuilder.RenameColumn(
                name: "SortingCode",
                schema: "dbo",
                table: "Data_SortingInfo",
                newName: "SentInstruction");

            migrationBuilder.AddColumn<string>(
                name: "CommandTarget",
                schema: "dbo",
                table: "Data_SortingInfo",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PackageCreationInstruction",
                schema: "dbo",
                table: "Data_SortingInfo",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ReceivedInstruction",
                schema: "dbo",
                table: "Data_SortingInfo",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedTime",
                schema: "dbo",
                table: "Data_SortingInfo",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "SendTime",
                schema: "dbo",
                table: "Data_SortingInfo",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

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
    }
}
