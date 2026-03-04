using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class PatientConfiguration : IEntityTypeConfiguration<Patient>
    {
        public void Configure(EntityTypeBuilder<Patient> builder)
        {
            builder.ToTable("Patients");

            builder.HasKey(p => p.PatientId);

            builder.Property(p => p.PatientId)
                   .ValueGeneratedOnAdd();

            builder.Property(p => p.MRN)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.HasIndex(p => p.MRN)
                   .IsUnique()
                   .HasDatabaseName("IX_Patients_MRN");

            builder.HasIndex(p => p.UserId)
                   .IsUnique()
                   .HasDatabaseName("IX_Patients_UserId");

            builder.Property(p => p.FullName)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(p => p.DateOfBirth)
                   .IsRequired();

            builder.Property(p => p.Gender)
                   .HasMaxLength(20);

            builder.Property(p => p.PhoneNumber)
                   .HasMaxLength(20);

            builder.Property(p => p.BloodGroup)
                   .HasMaxLength(10);

            builder.Property(p => p.EmergencyContact)
                   .HasMaxLength(20);

            builder.Property(p => p.IsDeleted)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.Property(p => p.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("SYSDATETIME()");

            // Global query filter for soft delete
            builder.HasQueryFilter(p => !p.IsDeleted);

            // Relationships
            builder.HasMany(p => p.PatientAllergies)
                   .WithOne(pa => pa.Patient)
                   .HasForeignKey(pa => pa.PatientId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Appointments)
                   .WithOne(apt => apt.Patient)
                   .HasForeignKey(apt => apt.PatientId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.PatientVitals)
                   .WithOne(pv => pv.Patient)
                   .HasForeignKey(pv => pv.PatientId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
