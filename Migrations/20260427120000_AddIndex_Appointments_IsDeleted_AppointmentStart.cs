using Microsoft.EntityFrameworkCore.Migrations;

namespace Axivora.Migrations
{
    public partial class AddIndex_Appointments_IsDeleted_AppointmentStart : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE NONCLUSTERED INDEX IX_Appointments_IsDeleted_AppointmentStart
                ON dbo.Appointments (IsDeleted, AppointmentStart DESC)
                INCLUDE (PatientId, DoctorId, StatusId, SlotId);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_Appointments_IsDeleted_AppointmentStart ON dbo.Appointments;
            ");
        }
    }
}
