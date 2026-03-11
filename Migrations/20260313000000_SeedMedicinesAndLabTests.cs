using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Axivora.Migrations
{
    /// <inheritdoc />
    public partial class SeedMedicinesAndLabTests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ?? Medicines ?????????????????????????????????????????????????????????
            // Insert only rows whose name does not already exist (idempotent).
            migrationBuilder.Sql("""
                INSERT INTO Medicines (MedicineName)
                SELECT v.Name
                FROM (VALUES
                    ('Paracetamol 500mg'),
                    ('Amoxicillin 250mg'),
                    ('Ibuprofen 400mg'),
                    ('Metformin 500mg'),
                    ('Atorvastatin 10mg'),
                    ('Omeprazole 20mg'),
                    ('Cetirizine 10mg'),
                    ('Azithromycin 500mg'),
                    ('Pantoprazole 40mg'),
                    ('Vitamin D3 1000IU')
                ) AS v(Name)
                WHERE NOT EXISTS (
                    SELECT 1 FROM Medicines m WHERE m.MedicineName = v.Name
                );
                """);

            // ?? Lab Tests ?????????????????????????????????????????????????????????
            migrationBuilder.Sql("""
                INSERT INTO LabTests (TestName)
                SELECT v.Name
                FROM (VALUES
                    ('Complete Blood Count (CBC)'),
                    ('Blood Glucose - Fasting'),
                    ('Lipid Profile'),
                    ('Liver Function Tests (LFT)'),
                    ('Kidney Function Tests (KFT)'),
                    ('Thyroid Stimulating Hormone (TSH)'),
                    ('Urine Routine Examination'),
                    ('HbA1c')
                ) AS v(Name)
                WHERE NOT EXISTS (
                    SELECT 1 FROM LabTests lt WHERE lt.TestName = v.Name
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM Medicines
                WHERE MedicineName IN (
                    'Paracetamol 500mg', 'Amoxicillin 250mg', 'Ibuprofen 400mg',
                    'Metformin 500mg', 'Atorvastatin 10mg', 'Omeprazole 20mg',
                    'Cetirizine 10mg', 'Azithromycin 500mg', 'Pantoprazole 40mg',
                    'Vitamin D3 1000IU'
                );
                """);

            migrationBuilder.Sql("""
                DELETE FROM LabTests
                WHERE TestName IN (
                    'Complete Blood Count (CBC)', 'Blood Glucose - Fasting',
                    'Lipid Profile', 'Liver Function Tests (LFT)',
                    'Kidney Function Tests (KFT)', 'Thyroid Stimulating Hormone (TSH)',
                    'Urine Routine Examination', 'HbA1c'
                );
                """);
        }
    }
}
