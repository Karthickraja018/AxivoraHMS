using Microsoft.EntityFrameworkCore;
using Axivora.Models;

namespace Axivora.Data
{
    public class AxivoraDbContext : DbContext
    {
        public AxivoraDbContext(DbContextOptions<AxivoraDbContext> options) : base(options) { }

        // Core Identity & Access
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        // Demographics & Clinical Setup
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<PatientAllergy> PatientAllergies { get; set; }

        // Staff & Departments
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<DoctorDepartment> DoctorDepartments { get; set; }

        // Scheduling & Visits
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
        public DbSet<AppointmentStatus> AppointmentStatuses { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        // Clinical Records
        public DbSet<ICDCode> ICDCodes { get; set; }
        public DbSet<Consultation> Consultations { get; set; }
        public DbSet<PatientVital> PatientVitals { get; set; }

        // Medications & Labs
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<LabTest> LabTests { get; set; }
        public DbSet<OrderedTest> OrderedTests { get; set; }

        // Audit Logging
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Automatically apply all IEntityTypeConfiguration implementations from the current assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AxivoraDbContext).Assembly);
        }
    }
}