using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Axivora.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentDescriptionAndPatientVitalsRestructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientVitals_Consultations_ConsultationId",
                table: "PatientVitals");

            migrationBuilder.DropIndex(
                name: "IX_PatientVitals_ConsultationId",
                table: "PatientVitals");

            migrationBuilder.DropColumn(
                name: "DiastolicBP",
                table: "PatientVitals");

            migrationBuilder.DropColumn(
                name: "HeartRate_BPM",
                table: "PatientVitals");

            migrationBuilder.DropColumn(
                name: "SpO2_Percentage",
                table: "PatientVitals");

            migrationBuilder.RenameColumn(
                name: "Weight_KG",
                table: "PatientVitals",
                newName: "Weight");

            migrationBuilder.RenameColumn(
                name: "Temperature_C",
                table: "PatientVitals",
                newName: "Temperature");

            migrationBuilder.RenameColumn(
                name: "SystolicBP",
                table: "PatientVitals",
                newName: "HeartRate");

            migrationBuilder.RenameColumn(
                name: "ConsultationId",
                table: "PatientVitals",
                newName: "PatientId");

            migrationBuilder.AddColumn<string>(
                name: "BloodPressure",
                table: "PatientVitals",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Height",
                table: "PatientVitals",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Departments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Departments",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientVitals_PatientId_RecordedAt",
                table: "PatientVitals",
                columns: new[] { "PatientId", "RecordedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_PatientVitals_Patients_PatientId",
                table: "PatientVitals",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "PatientId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientVitals_Patients_PatientId",
                table: "PatientVitals");

            migrationBuilder.DropIndex(
                name: "IX_PatientVitals_PatientId_RecordedAt",
                table: "PatientVitals");

            migrationBuilder.DropColumn(
                name: "BloodPressure",
                table: "PatientVitals");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "PatientVitals");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Departments");

            migrationBuilder.RenameColumn(
                name: "Weight",
                table: "PatientVitals",
                newName: "Weight_KG");

            migrationBuilder.RenameColumn(
                name: "Temperature",
                table: "PatientVitals",
                newName: "Temperature_C");

            migrationBuilder.RenameColumn(
                name: "PatientId",
                table: "PatientVitals",
                newName: "ConsultationId");

            migrationBuilder.RenameColumn(
                name: "HeartRate",
                table: "PatientVitals",
                newName: "SystolicBP");

            migrationBuilder.AddColumn<int>(
                name: "DiastolicBP",
                table: "PatientVitals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HeartRate_BPM",
                table: "PatientVitals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpO2_Percentage",
                table: "PatientVitals",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientVitals_ConsultationId",
                table: "PatientVitals",
                column: "ConsultationId");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientVitals_Consultations_ConsultationId",
                table: "PatientVitals",
                column: "ConsultationId",
                principalTable: "Consultations",
                principalColumn: "ConsultationId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
