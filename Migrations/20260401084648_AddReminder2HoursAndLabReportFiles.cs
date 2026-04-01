using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Axivora.Migrations
{
    /// <inheritdoc />
    public partial class AddReminder2HoursAndLabReportFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReportContentType",
                table: "OrderedTests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportFileName",
                table: "OrderedTests",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportFilePath",
                table: "OrderedTests",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReportSizeBytes",
                table: "OrderedTests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Reminder2HoursSent",
                table: "Appointments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportContentType",
                table: "OrderedTests");

            migrationBuilder.DropColumn(
                name: "ReportFileName",
                table: "OrderedTests");

            migrationBuilder.DropColumn(
                name: "ReportFilePath",
                table: "OrderedTests");

            migrationBuilder.DropColumn(
                name: "ReportSizeBytes",
                table: "OrderedTests");

            migrationBuilder.DropColumn(
                name: "Reminder2HoursSent",
                table: "Appointments");
        }
    }
}
