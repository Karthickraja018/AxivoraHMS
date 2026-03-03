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

            // Configure all entities
            ConfigureUser(modelBuilder);
            ConfigureRole(modelBuilder);
            ConfigureUserRole(modelBuilder);
            ConfigureAddress(modelBuilder);
            ConfigurePatient(modelBuilder);
            ConfigurePatientAllergy(modelBuilder);
            ConfigureDoctor(modelBuilder);
            ConfigureDepartment(modelBuilder);
            ConfigureDoctorDepartment(modelBuilder);
            ConfigureDoctorSchedule(modelBuilder);
            ConfigureAppointmentStatus(modelBuilder);
            ConfigureAppointment(modelBuilder);
            ConfigureICDCode(modelBuilder);
            ConfigureConsultation(modelBuilder);
            ConfigurePatientVital(modelBuilder);
            ConfigureMedicine(modelBuilder);
            ConfigurePrescription(modelBuilder);
            ConfigureLabTest(modelBuilder);
            ConfigureOrderedTest(modelBuilder);
            ConfigureAuditLog(modelBuilder);
        }

        private void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.UserId);
                
                entity.Property(u => u.UserId)
                      .ValueGeneratedOnAdd();

                entity.Property(u => u.Email)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.HasIndex(u => u.Email)
                      .IsUnique();

                entity.Property(u => u.Password)
                      .IsRequired()
                      .HasMaxLength(255)
                      .HasColumnName("PasswordHash");

                entity.Property(u => u.IsActive)
                      .IsRequired()
                      .HasDefaultValue(true);

                entity.Property(u => u.IsDeleted)
                      .IsRequired()
                      .HasDefaultValue(false);

                entity.Property(u => u.CreatedAt)
                      .IsRequired()
                      .HasDefaultValueSql("SYSDATETIME()");

                entity.Property(u => u.UpdatedAt)
                      .HasDefaultValueSql("SYSDATETIME()");

                entity.HasMany(u => u.UserRoles)
                      .WithOne(ur => ur.User)
                      .HasForeignKey(ur => ur.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(u => u.Patient)
                      .WithOne(p => p.User)
                      .HasForeignKey<Patient>(p => p.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(u => u.Doctor)
                      .WithOne(d => d.User)
                      .HasForeignKey<Doctor>(d => d.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.AuditLogs)
                      .WithOne(a => a.User)
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureRole(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasKey(r => r.RoleId);

                entity.Property(r => r.RoleId)
                      .ValueGeneratedOnAdd();

                entity.Property(r => r.RoleName)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.HasIndex(r => r.RoleName)
                      .IsUnique();

                entity.HasMany(r => r.UserRoles)
                      .WithOne(ur => ur.Role)
                      .HasForeignKey(ur => ur.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureUserRole(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");
                entity.HasKey(ur => ur.UserRoleId);

                entity.Property(ur => ur.UserRoleId)
                      .ValueGeneratedOnAdd();

                entity.Property(ur => ur.UserId)
                      .IsRequired();

                entity.Property(ur => ur.RoleId)
                      .IsRequired();

                entity.HasIndex(ur => new { ur.UserId, ur.RoleId })
                      .IsUnique()
                      .HasDatabaseName("UQ_UserRole");
            });
        }

        private void ConfigureAddress(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Address>(entity =>
            {
                entity.ToTable("Addresses");
                entity.HasKey(a => a.AddressId);

                entity.Property(a => a.AddressId)
                      .ValueGeneratedOnAdd();

                entity.Property(a => a.AddressLine1)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(a => a.AddressLine2)
                      .HasMaxLength(200);

                entity.Property(a => a.City)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(a => a.State)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(a => a.PostalCode)
                      .HasMaxLength(20);

                entity.Property(a => a.Country)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(a => a.CreatedAt)
                      .HasDefaultValueSql("SYSDATETIME()");
            });
        }

        private void ConfigurePatient(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.ToTable("Patients");
                entity.HasKey(p => p.PatientId);

                entity.Property(p => p.PatientId)
                      .ValueGeneratedOnAdd();

                entity.Property(p => p.MRN)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.HasIndex(p => p.MRN)
                      .IsUnique();

                entity.HasIndex(p => p.UserId)
                      .IsUnique();

                entity.Property(p => p.FullName)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(p => p.DateOfBirth)
                      .IsRequired();

                entity.Property(p => p.Gender)
                      .HasMaxLength(20);

                entity.Property(p => p.PhoneNumber)
                      .HasMaxLength(20);

                entity.Property(p => p.BloodGroup)
                      .HasMaxLength(10);

                entity.Property(p => p.EmergencyContact)
                      .HasMaxLength(20);

                entity.Property(p => p.IsDeleted)
                      .HasDefaultValue(true);

                entity.Property(p => p.CreatedAt)
                      .HasDefaultValueSql("SYSDATETIME()");

                entity.HasOne(p => p.Address)
                      .WithMany(a => a.Patients)
                      .HasForeignKey(p => p.AddressId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(p => p.PatientAllergies)
                      .WithOne(pa => pa.Patient)
                      .HasForeignKey(pa => pa.PatientId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(p => p.Appointments)
                      .WithOne(apt => apt.Patient)
                      .HasForeignKey(apt => apt.PatientId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(p => p.PatientVitals)
                      .WithOne(pv => pv.Patient)
                      .HasForeignKey(pv => pv.PatientId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigurePatientAllergy(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PatientAllergy>(entity =>
            {
                entity.ToTable("PatientAllergies");
                entity.HasKey(pa => pa.AllergyId);

                entity.Property(pa => pa.AllergyId)
                      .ValueGeneratedOnAdd();

                entity.Property(pa => pa.AllergenName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(pa => pa.Severity)
                      .HasMaxLength(50);

                entity.Property(pa => pa.Reaction)
                      .HasMaxLength(200);

                entity.Property(pa => pa.RecordedAt)
                      .HasDefaultValueSql("SYSDATETIME()");
            });
        }

        private void ConfigureDoctor(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Doctor>(entity =>
            {
                entity.ToTable("Doctors");
                entity.HasKey(d => d.DoctorId);

                entity.Property(d => d.DoctorId)
                      .ValueGeneratedOnAdd();

                entity.Property(d => d.LicenseNumber)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasIndex(d => d.LicenseNumber)
                      .IsUnique();

                entity.HasIndex(d => d.UserId)
                      .IsUnique();

                entity.Property(d => d.FullName)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(d => d.Qualification)
                      .HasMaxLength(150);

                entity.Property(d => d.ExperienceYears);

                entity.Property(d => d.IsActive)
                      .HasDefaultValue(true);

                entity.Property(d => d.IsDeleted)
                      .HasDefaultValue(false);

                entity.Property(d => d.CreatedAt)
                      .HasDefaultValueSql("SYSDATETIME()");

                entity.HasOne(d => d.Address)
                      .WithMany(a => a.Doctors)
                      .HasForeignKey(d => d.AddressId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(d => d.DoctorDepartments)
                      .WithOne(dd => dd.Doctor)
                      .HasForeignKey(dd => dd.DoctorId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(d => d.DoctorSchedules)
                      .WithOne(ds => ds.Doctor)
                      .HasForeignKey(ds => ds.DoctorId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(d => d.Appointments)
                      .WithOne(apt => apt.Doctor)
                      .HasForeignKey(apt => apt.DoctorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureDepartment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Department>(entity =>
            {
                entity.ToTable("Departments");
                entity.HasKey(d => d.DepartmentId);

                entity.Property(d => d.DepartmentId)
                      .ValueGeneratedOnAdd();

                entity.Property(d => d.DepartmentName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasIndex(d => d.DepartmentName)
                      .IsUnique();

                entity.HasMany(d => d.DoctorDepartments)
                      .WithOne(dd => dd.Department)
                      .HasForeignKey(dd => dd.DepartmentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureDoctorDepartment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DoctorDepartment>(entity =>
            {
                entity.ToTable("DoctorDepartments");
                entity.HasKey(dd => dd.Id);

                entity.Property(dd => dd.Id)
                      .ValueGeneratedOnAdd();

                entity.HasIndex(dd => new { dd.DoctorId, dd.DepartmentId })
                      .IsUnique()
                      .HasDatabaseName("UQ_DoctorDepartment");
            });
        }

        private void ConfigureDoctorSchedule(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DoctorSchedule>(entity =>
            {
                entity.ToTable("DoctorSchedules");
                entity.HasKey(ds => ds.ScheduleId);

                entity.Property(ds => ds.ScheduleId)
                      .ValueGeneratedOnAdd();

                entity.Property(ds => ds.DayOfWeek)
                      .IsRequired();

                entity.Property(ds => ds.StartTime)
                      .IsRequired();

                entity.Property(ds => ds.EndTime)
                      .IsRequired();

                entity.Property(ds => ds.SlotDurationMinutes)
                      .HasDefaultValue(15);

                entity.Property(ds => ds.IsActive)
                      .HasDefaultValue(true);
            });
        }

        private void ConfigureAppointmentStatus(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppointmentStatus>(entity =>
            {
                entity.ToTable("AppointmentStatus");
                entity.HasKey(ast => ast.StatusId);

                entity.Property(ast => ast.StatusId)
                      .ValueGeneratedOnAdd();

                entity.Property(ast => ast.StatusName)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.HasIndex(ast => ast.StatusName)
                      .IsUnique();

                entity.HasMany(ast => ast.Appointments)
                      .WithOne(apt => apt.Status)
                      .HasForeignKey(apt => apt.StatusId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureAppointment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.ToTable("Appointments");
                entity.HasKey(apt => apt.AppointmentId);

                entity.Property(apt => apt.AppointmentId)
                      .ValueGeneratedOnAdd();

                entity.Property(apt => apt.AppointmentStart)
                      .IsRequired();

                entity.Property(apt => apt.AppointmentEnd)
                      .IsRequired();

                entity.Property(apt => apt.Reason)
                      .HasMaxLength(500);

                entity.Property(apt => apt.IsDeleted)
                      .HasDefaultValue(false);

                entity.Property(apt => apt.CreatedAt)
                      .HasDefaultValueSql("SYSDATETIME()");

                entity.HasOne(apt => apt.Consultation)
                      .WithOne(c => c.Appointment)
                      .HasForeignKey<Consultation>(c => c.AppointmentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureICDCode(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ICDCode>(entity =>
            {
                entity.ToTable("ICDCodes");
                entity.HasKey(icd => icd.ICDId);

                entity.Property(icd => icd.ICDId)
                      .ValueGeneratedOnAdd();

                entity.Property(icd => icd.Code)
                      .IsRequired()
                      .HasMaxLength(10);

                entity.HasIndex(icd => icd.Code)
                      .IsUnique();

                entity.Property(icd => icd.Description)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.HasMany(icd => icd.Consultations)
                      .WithOne(c => c.ICDCode)
                      .HasForeignKey(c => c.ICDId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureConsultation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Consultation>(entity =>
            {
                entity.ToTable("Consultations");
                entity.HasKey(c => c.ConsultationId);

                entity.Property(c => c.ConsultationId)
                      .ValueGeneratedOnAdd();

                entity.HasIndex(c => c.AppointmentId)
                      .IsUnique();

                entity.Property(c => c.ChiefComplaint)
                      .HasMaxLength(1000);

                entity.Property(c => c.Examination)
                      .HasMaxLength(1000);

                entity.Property(c => c.DiagnosisNotes)
                      .HasMaxLength(500);

                entity.Property(c => c.TreatmentPlan)
                      .HasMaxLength(1000);

                entity.Property(c => c.CreatedAt)
                      .HasDefaultValueSql("SYSDATETIME()");

                entity.HasMany(c => c.PatientVitals)
                      .WithOne(pv => pv.Consultation)
                      .HasForeignKey(pv => pv.ConsultationId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(c => c.Prescriptions)
                      .WithOne(p => p.Consultation)
                      .HasForeignKey(p => p.ConsultationId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(c => c.OrderedTests)
                      .WithOne(ot => ot.Consultation)
                      .HasForeignKey(ot => ot.ConsultationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigurePatientVital(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PatientVital>(entity =>
            {
                entity.ToTable("PatientVitals");
                entity.HasKey(pv => pv.VitalId);

                entity.Property(pv => pv.VitalId)
                      .ValueGeneratedOnAdd();

                entity.Property(pv => pv.Temperature_C)
                      .HasPrecision(4, 2);

                entity.Property(pv => pv.Weight_KG)
                      .HasPrecision(5, 2);

                entity.Property(pv => pv.RecordedAt)
                      .HasDefaultValueSql("SYSDATETIME()");
            });
        }

        private void ConfigureMedicine(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Medicine>(entity =>
            {
                entity.ToTable("Medicines");
                entity.HasKey(m => m.MedicineId);

                entity.Property(m => m.MedicineId)
                      .ValueGeneratedOnAdd();

                entity.Property(m => m.MedicineName)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.HasIndex(m => m.MedicineName)
                      .IsUnique();

                entity.HasMany(m => m.Prescriptions)
                      .WithOne(p => p.Medicine)
                      .HasForeignKey(p => p.MedicineId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigurePrescription(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Prescription>(entity =>
            {
                entity.ToTable("Prescriptions");
                entity.HasKey(p => p.PrescriptionId);

                entity.Property(p => p.PrescriptionId)
                      .ValueGeneratedOnAdd();

                entity.Property(p => p.Dosage)
                      .HasMaxLength(50);

                entity.Property(p => p.Frequency)
                      .HasMaxLength(50);

                entity.Property(p => p.Route)
                      .HasMaxLength(50);

                entity.Property(p => p.Instructions)
                      .HasMaxLength(200);
            });
        }

        private void ConfigureLabTest(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LabTest>(entity =>
            {
                entity.ToTable("LabTests");
                entity.HasKey(lt => lt.LabTestId);

                entity.Property(lt => lt.LabTestId)
                      .ValueGeneratedOnAdd();

                entity.Property(lt => lt.TestName)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.HasIndex(lt => lt.TestName)
                      .IsUnique();

                entity.HasMany(lt => lt.OrderedTests)
                      .WithOne(ot => ot.LabTest)
                      .HasForeignKey(ot => ot.LabTestId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureOrderedTest(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderedTest>(entity =>
            {
                entity.ToTable("OrderedTests");
                entity.HasKey(ot => ot.OrderedTestId);

                entity.Property(ot => ot.OrderedTestId)
                      .ValueGeneratedOnAdd();

                entity.Property(ot => ot.Status)
                      .HasMaxLength(50)
                      .HasDefaultValue("Pending");
            });
        }

        private void ConfigureAuditLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLogs");
                entity.HasKey(al => al.AuditId);

                entity.Property(al => al.AuditId)
                      .ValueGeneratedOnAdd();

                entity.Property(al => al.Action)
                      .HasMaxLength(100);

                entity.Property(al => al.EntityName)
                      .HasMaxLength(100);

                entity.Property(al => al.CreatedAt)
                      .HasDefaultValueSql("SYSDATETIME()");
            });
        }
    }
}