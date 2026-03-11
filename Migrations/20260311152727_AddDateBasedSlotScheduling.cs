using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Axivora.Migrations
{
    /// <inheritdoc />
    public partial class AddDateBasedSlotScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SlotId",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DoctorAvailabilityTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    SlotDurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 15),
                    EffectiveFromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorAvailabilityTemplates", x => x.Id);
                    table.CheckConstraint("CHK_AvailabilityTemplate_DayOfWeek", "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6");
                    table.CheckConstraint("CHK_AvailabilityTemplate_SlotDuration", "[SlotDurationMinutes] >= 5 AND [SlotDurationMinutes] <= 120");
                    table.CheckConstraint("CHK_AvailabilityTemplate_Times", "[EndTime] > [StartTime]");
                    table.ForeignKey(
                        name: "FK_DoctorAvailabilityTemplates_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoctorAvailabilityDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    SlotDurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 15),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Open"),
                    SourceTemplateId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorAvailabilityDays", x => x.Id);
                    table.CheckConstraint("CHK_AvailabilityDay_Times", "[EndTime] > [StartTime]");
                    table.ForeignKey(
                        name: "FK_DoctorAvailabilityDays_DoctorAvailabilityTemplates_SourceTemplateId",
                        column: x => x.SourceTemplateId,
                        principalTable: "DoctorAvailabilityTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DoctorAvailabilityDays_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    AvailabilityDayId = table.Column<int>(type: "int", nullable: false),
                    SlotStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SlotEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Available"),
                    AppointmentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentSlots", x => x.Id);
                    table.CheckConstraint("CHK_AppointmentSlot_Times", "[SlotEnd] > [SlotStart]");
                    table.ForeignKey(
                        name: "FK_AppointmentSlots_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "AppointmentId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AppointmentSlots_DoctorAvailabilityDays_AvailabilityDayId",
                        column: x => x.AvailabilityDayId,
                        principalTable: "DoctorAvailabilityDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentSlots_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentSlots_AvailabilityDayId_Status",
                table: "AppointmentSlots",
                columns: new[] { "AvailabilityDayId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentSlots_DoctorId_SlotStart",
                table: "AppointmentSlots",
                columns: new[] { "DoctorId", "SlotStart" });

            migrationBuilder.CreateIndex(
                name: "UQ_AppointmentSlots_AppointmentId",
                table: "AppointmentSlots",
                column: "AppointmentId",
                unique: true,
                filter: "[AppointmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityDays_DoctorId_Date_Status",
                table: "DoctorAvailabilityDays",
                columns: new[] { "DoctorId", "Date", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAvailabilityDays_SourceTemplateId",
                table: "DoctorAvailabilityDays",
                column: "SourceTemplateId");

            migrationBuilder.CreateIndex(
                name: "UQ_AvailabilityDays_DoctorId_Date",
                table: "DoctorAvailabilityDays",
                columns: new[] { "DoctorId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityTemplates_DoctorId_DayOfWeek",
                table: "DoctorAvailabilityTemplates",
                columns: new[] { "DoctorId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityTemplates_DoctorId_IsActive",
                table: "DoctorAvailabilityTemplates",
                columns: new[] { "DoctorId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentSlots");

            migrationBuilder.DropTable(
                name: "DoctorAvailabilityDays");

            migrationBuilder.DropTable(
                name: "DoctorAvailabilityTemplates");

            migrationBuilder.DropColumn(
                name: "SlotId",
                table: "Appointments");
        }
    }
}
