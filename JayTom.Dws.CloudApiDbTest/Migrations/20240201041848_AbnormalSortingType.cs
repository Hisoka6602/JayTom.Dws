using Microsoft.EntityFrameworkCore.Migrations;

namespace JayTom.Dws.CloudApiDbTest.Migrations
{
    public partial class AbnormalSortingType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AbnormalSortingType",
                schema: "dbo",
                table: "Data_SortingInfo",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbnormalSortingType",
                schema: "dbo",
                table: "Data_SortingInfo");
        }
    }
}
