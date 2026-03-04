using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.ToTable("Appointments");

            builder.HasKey(apt => apt.AppointmentId);

            builder.Property(apt => apt.AppointmentId)
                   .ValueGeneratedOnAdd();

            builder.Property(apt => apt.PatientId)
                   .IsRequired();

            builder.Property(apt => apt.DoctorId)
                   .IsRequired();

            builder.Property(apt => apt.StatusId)
                   .IsRequired();

            builder.Property(apt => apt.AppointmentStart)
                   .IsRequired();

            builder.Property(apt => apt.AppointmentEnd)
                   .IsRequired();

            builder.Property(apt => apt.Reason)
                   .HasMaxLength(500);

            builder.Property(apt => apt.IsDeleted)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.Property(apt => apt.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("SYSDATETIME()");

            // Global query filter for soft delete
            builder.HasQueryFilter(apt => !apt.IsDeleted);

            // Indexes for quick lookup
            builder.HasIndex(apt => apt.PatientId)
                   .HasDatabaseName("IX_Appointments_PatientId");

            builder.HasIndex(apt => apt.DoctorId)
                   .HasDatabaseName("IX_Appointments_DoctorId");

            builder.HasIndex(apt => apt.AppointmentStart)
                   .HasDatabaseName("IX_Appointments_AppointmentStart");

            builder.HasIndex(apt => new { apt.DoctorId, apt.AppointmentStart })
                   .HasDatabaseName("IX_Appointments_DoctorId_AppointmentStart");

            // Relationships
            builder.HasOne(apt => apt.Consultation)
                   .WithOne(c => c.Appointment)
                   .HasForeignKey<Consultation>(c => c.AppointmentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
