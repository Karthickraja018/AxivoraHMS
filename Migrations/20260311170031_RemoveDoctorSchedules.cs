using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Axivora.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDoctorSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorSchedules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoctorSchedules",
                columns: table => new
                {
                    ScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SlotDurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 15),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorSchedules", x => x.ScheduleId);
                    table.CheckConstraint("CHK_DoctorSchedule_Times", "\"EndTime\" > \"StartTime\"");
                    table.CheckConstraint("CHK_DoctorSchedules_DayOfWeek", "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6");
                    table.ForeignKey(
                        name: "FK_DoctorSchedules_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorSchedules_DoctorId_DayOfWeek",
                table: "DoctorSchedules",
                columns: new[] { "DoctorId", "DayOfWeek" });
        }
    }
}
