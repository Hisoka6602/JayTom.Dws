using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JayTom.Dws.LicenseApiDbTest.Migrations
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
                name: "App_LicensePermissionTemplateInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TemplateName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreateBy = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifyTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifyIp = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_App_LicensePermissionTemplateInfo", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Code_LicenseGroupInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GroupName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifyTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifyIp = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Code_LicenseGroupInfo", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Sys_LicenseUserInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Pid = table.Column<long>(type: "bigint", nullable: false),
                    UserCode = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PassWord = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UserIcon = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifyTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifyIp = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_LicenseUserInfo", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "App_LicenseApplicationInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LicensePermissionTemplateId = table.Column<long>(type: "bigint", nullable: true),
                    ApplicationName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifyTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifyIp = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_App_LicenseApplicationInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_App_LicenseApplicationInfo_App_LicensePermissionTemplateInfo~",
                        column: x => x.LicensePermissionTemplateId,
                        principalSchema: "dbo",
                        principalTable: "App_LicensePermissionTemplateInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "App_LicenseAppLicenseInfo",
                schema: "dbo",
                columns: table => new
                {
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
                constraints: table =>
                {
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

            migrationBuilder.CreateTable(
                name: "Code_LicenseCodeInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    LicensePermissionTemplateInfoId = table.Column<long>(type: "bigint", nullable: true),
                    LicenseGroupInfoId = table.Column<long>(type: "bigint", nullable: true),
                    LicenseCode = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaxClientCount = table.Column<int>(type: "int", nullable: false),
                    ActivatedClientCount = table.Column<int>(type: "int", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ClientName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsAvailable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifyTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifyIp = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Code_LicenseCodeInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Code_LicenseCodeInfo_App_LicensePermissionTemplateInfo_Licen~",
                        column: x => x.LicensePermissionTemplateInfoId,
                        principalSchema: "dbo",
                        principalTable: "App_LicensePermissionTemplateInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Code_LicenseCodeInfo_Code_LicenseGroupInfo_LicenseGroupInfoId",
                        column: x => x.LicenseGroupInfoId,
                        principalSchema: "dbo",
                        principalTable: "Code_LicenseGroupInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Code_LicenseCodeInfo_Sys_LicenseUserInfo_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Sys_LicenseUserInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Sys_LicenseUserDetailsInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CompanyAddress = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactEmail = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContractFilePath = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BusinessLicenseFilePath = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifyTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifyIp = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_LicenseUserDetailsInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sys_LicenseUserDetailsInfo_Sys_LicenseUserInfo_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Sys_LicenseUserInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "App_LicenseFeatureInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LicenseApplicationInfoId = table.Column<long>(type: "bigint", nullable: false),
                    Pid = table.Column<long>(type: "bigint", nullable: false),
                    FeatureName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FeatureGuid = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifyTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifyIp = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_App_LicenseFeatureInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_App_LicenseFeatureInfo_App_LicenseApplicationInfo_LicenseApp~",
                        column: x => x.LicenseApplicationInfoId,
                        principalSchema: "dbo",
                        principalTable: "App_LicenseApplicationInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Code_LicenseClientBindingInfo",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LicenseCodeId = table.Column<long>(type: "bigint", nullable: false),
                    MachineCode = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FirstActivatedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastVerifiedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifyTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifyIp = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Code_LicenseClientBindingInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Code_LicenseClientBindingInfo_Code_LicenseCodeInfo_LicenseCo~",
                        column: x => x.LicenseCodeId,
                        principalSchema: "dbo",
                        principalTable: "Code_LicenseCodeInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_App_LicenseApplicationInfo_LicensePermissionTemplateId",
                schema: "dbo",
                table: "App_LicenseApplicationInfo",
                column: "LicensePermissionTemplateId",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_App_LicenseFeatureInfo_LicenseApplicationInfoId",
                schema: "dbo",
                table: "App_LicenseFeatureInfo",
                column: "LicenseApplicationInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_Code_LicenseClientBindingInfo_LicenseCodeId",
                schema: "dbo",
                table: "Code_LicenseClientBindingInfo",
                column: "LicenseCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Code_LicenseCodeInfo_LicenseGroupInfoId",
                schema: "dbo",
                table: "Code_LicenseCodeInfo",
                column: "LicenseGroupInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_Code_LicenseCodeInfo_LicensePermissionTemplateInfoId",
                schema: "dbo",
                table: "Code_LicenseCodeInfo",
                column: "LicensePermissionTemplateInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_Code_LicenseCodeInfo_UserId",
                schema: "dbo",
                table: "Code_LicenseCodeInfo",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_LicenseUserDetailsInfo_UserId",
                schema: "dbo",
                table: "Sys_LicenseUserDetailsInfo",
                column: "UserId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "App_LicenseAppLicenseInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "App_LicenseFeatureInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Code_LicenseClientBindingInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Sys_LicenseUserDetailsInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "App_LicenseApplicationInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Code_LicenseCodeInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "App_LicensePermissionTemplateInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Code_LicenseGroupInfo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Sys_LicenseUserInfo",
                schema: "dbo");
        }
    }
}
