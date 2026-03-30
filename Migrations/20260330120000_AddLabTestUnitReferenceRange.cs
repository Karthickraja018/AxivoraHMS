using Microsoft.EntityFrameworkCore.Migrations;

namespace Axivora.Migrations
{
    public partial class AddLabTestUnitReferenceRange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferenceRange",
                table: "LabTests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "LabTests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferenceRange",
                table: "LabTests");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "LabTests");
        }
    }
}
