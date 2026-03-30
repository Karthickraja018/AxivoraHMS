using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Axivora.Migrations
{
    /// <inheritdoc />
    public partial class RedesignDoctorAvailabilityIndexWithUserRoleFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_AvailabilityDays_DoctorId_Date",
                table: "DoctorAvailabilityDays");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityDays_DoctorId_Date",
                table: "DoctorAvailabilityDays",
                columns: new[] { "DoctorId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AvailabilityDays_DoctorId_Date",
                table: "DoctorAvailabilityDays");

            migrationBuilder.CreateIndex(
                name: "UQ_AvailabilityDays_DoctorId_Date",
                table: "DoctorAvailabilityDays",
                columns: new[] { "DoctorId", "Date" },
                unique: true);
        }
    }
}
