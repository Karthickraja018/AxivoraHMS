using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Axivora.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingViews : Migration
    {
        private const string CreateAppointmentReportView = """
            CREATE VIEW vw_AppointmentReport AS
            SELECT
                a.AppointmentId,
                a.AppointmentStart,
                a.AppointmentEnd,
                a.Reason,
                s.StatusName,
                p.FullName        AS PatientName,
                p.PhoneNumber     AS PatientPhone,
                p.MRN,
                d.FullName        AS DoctorName,
                dep.DepartmentName,
                CASE WHEN c.ConsultationId IS NOT NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS HasConsultation
            FROM Appointments a
            JOIN AppointmentStatus  s   ON a.StatusId      = s.StatusId
            JOIN Patients           p   ON a.PatientId     = p.PatientId
            JOIN Doctors            d   ON a.DoctorId      = d.DoctorId
            LEFT JOIN DoctorDepartments dd  ON d.DoctorId  = dd.DoctorId
            LEFT JOIN Departments   dep ON dd.DepartmentId = dep.DepartmentId
            LEFT JOIN Consultations c   ON a.AppointmentId = c.AppointmentId
            WHERE a.IsDeleted = 0;
            """;

        private const string CreateDoctorWorkloadReportView = """
            CREATE VIEW vw_DoctorWorkloadReport AS
            SELECT
                d.DoctorId,
                d.FullName        AS DoctorName,
                d.Qualification,
                dep.DepartmentName,
                COUNT(DISTINCT a.AppointmentId)                                          AS TotalAppointments,
                SUM(CASE WHEN s.StatusName = 'Completed'  THEN 1 ELSE 0 END)            AS CompletedAppointments,
                SUM(CASE WHEN s.StatusName = 'Cancelled'  THEN 1 ELSE 0 END)            AS CancelledAppointments,
                COUNT(DISTINCT c.ConsultationId)                                         AS TotalConsultations
            FROM Doctors d
            LEFT JOIN DoctorDepartments dd  ON d.DoctorId      = dd.DoctorId
            LEFT JOIN Departments   dep ON dd.DepartmentId     = dep.DepartmentId
            LEFT JOIN Appointments  a   ON d.DoctorId          = a.DoctorId AND a.IsDeleted = 0
            LEFT JOIN AppointmentStatus s   ON a.StatusId      = s.StatusId
            LEFT JOIN Consultations c   ON a.AppointmentId     = c.AppointmentId
            WHERE d.IsDeleted = 0
            GROUP BY d.DoctorId, d.FullName, d.Qualification, dep.DepartmentName;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(CreateAppointmentReportView);
            migrationBuilder.Sql(CreateDoctorWorkloadReportView);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_DoctorWorkloadReport;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_AppointmentReport;");
        }
    }
}
