using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Axivora.Migrations
{
    /// <inheritdoc />
    public partial class admin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionFeedback_Consultations_ConsultationId",
                table: "SessionFeedback");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionFeedback_Patients_PatientId",
                table: "SessionFeedback");

            migrationBuilder.RenameTable(
                name: "SessionFeedback",
                newName: "SessionFeedbacks");

            migrationBuilder.AlterColumn<int>(
                name: "PatientId",
                table: "SessionFeedbacks",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ConsultationId",
                table: "SessionFeedbacks",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FeedbackId",
                table: "SessionFeedbacks",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "SessionFeedbacks",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SessionFeedbacks",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSDATETIME()");

            migrationBuilder.AddColumn<bool>(
                name: "IsEdited",
                table: "SessionFeedbacks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "SessionFeedbacks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SessionFeedbacks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SessionFeedbacks",
                table: "SessionFeedbacks",
                column: "FeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionFeedbacks_PatientId",
                table: "SessionFeedbacks",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "UQ_SessionFeedbacks_ConsultationId",
                table: "SessionFeedbacks",
                column: "ConsultationId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CHK_SessionFeedbacks_Rating",
                table: "SessionFeedbacks",
                sql: "[Rating] >= 1 AND [Rating] <= 5");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionFeedbacks_Consultations_ConsultationId",
                table: "SessionFeedbacks",
                column: "ConsultationId",
                principalTable: "Consultations",
                principalColumn: "ConsultationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionFeedbacks_Patients_PatientId",
                table: "SessionFeedbacks",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "PatientId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionFeedbacks_Consultations_ConsultationId",
                table: "SessionFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionFeedbacks_Patients_PatientId",
                table: "SessionFeedbacks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SessionFeedbacks",
                table: "SessionFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_SessionFeedbacks_PatientId",
                table: "SessionFeedbacks");

            migrationBuilder.DropIndex(
                name: "UQ_SessionFeedbacks_ConsultationId",
                table: "SessionFeedbacks");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_SessionFeedbacks_Rating",
                table: "SessionFeedbacks");

            migrationBuilder.DropColumn(
                name: "FeedbackId",
                table: "SessionFeedbacks");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "SessionFeedbacks");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SessionFeedbacks");

            migrationBuilder.DropColumn(
                name: "IsEdited",
                table: "SessionFeedbacks");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "SessionFeedbacks");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SessionFeedbacks");

            migrationBuilder.RenameTable(
                name: "SessionFeedbacks",
                newName: "SessionFeedback");

            migrationBuilder.AlterColumn<int>(
                name: "PatientId",
                table: "SessionFeedback",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "ConsultationId",
                table: "SessionFeedback",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionFeedback_Consultations_ConsultationId",
                table: "SessionFeedback",
                column: "ConsultationId",
                principalTable: "Consultations",
                principalColumn: "ConsultationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionFeedback_Patients_PatientId",
                table: "SessionFeedback",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "PatientId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
