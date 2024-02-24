using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LicenseDBTest.Migrations {

    public partial class LicenseAppLicenseInfo : Migration {

        protected override void Up(MigrationBuilder migrationBuilder) {
            /*migrationBuilder.DropForeignKey(
                name: "FK_Code_LicenseClientBindingInfo_Sys_LicenseUserInfo_Id",
                schema: "dbo",
                table: "Code_LicenseClientBindingInfo");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "dbo",
                table: "Code_LicenseClientBindingInfo");*/

            migrationBuilder.CreateTable(
                name: "App_LicenseAppLicenseInfo",
                schema: "dbo",
                columns: table => new {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    LicensePermissionTemplateInfoId = table.Column<long>(type: "bigint", nullable: true),
                    MaxLicenseCodeCount = table.Column<int>(type: "int", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifyTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifyIp = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table => {
                    table.PrimaryKey("PK_App_LicenseAppLicenseInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_App_LicenseAppLicenseInfo_App_LicensePermissionTemplateInfo_~",
                        column: x => x.LicensePermissionTemplateInfoId,
                        principalSchema: "dbo",
                        principalTable: "App_LicensePermissionTemplateInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_App_LicenseAppLicenseInfo_Sys_LicenseUserInfo_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Sys_LicenseUserInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_App_LicenseAppLicenseInfo_LicensePermissionTemplateInfoId",
                schema: "dbo",
                table: "App_LicenseAppLicenseInfo",
                column: "LicensePermissionTemplateInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_App_LicenseAppLicenseInfo_UserId",
                schema: "dbo",
                table: "App_LicenseAppLicenseInfo",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "App_LicenseAppLicenseInfo",
                schema: "dbo");

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                schema: "dbo",
                table: "Code_LicenseClientBindingInfo",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddForeignKey(
                name: "FK_Code_LicenseClientBindingInfo_Sys_LicenseUserInfo_Id",
                schema: "dbo",
                table: "Code_LicenseClientBindingInfo",
                column: "Id",
                principalSchema: "dbo",
                principalTable: "Sys_LicenseUserInfo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}