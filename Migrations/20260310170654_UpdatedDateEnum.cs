using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Axivora.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedDateEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CHK_DoctorSchedules_DayOfWeek",
                table: "DoctorSchedules",
                sql: "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CHK_DoctorSchedules_DayOfWeek",
                table: "DoctorSchedules");
        }
    }
}
