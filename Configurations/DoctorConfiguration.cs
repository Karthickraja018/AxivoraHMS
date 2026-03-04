using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
    {
        public void Configure(EntityTypeBuilder<Doctor> builder)
        {
            builder.ToTable("Doctors");

            builder.HasKey(d => d.DoctorId);

            builder.Property(d => d.DoctorId)
                   .ValueGeneratedOnAdd();

            builder.Property(d => d.UserId)
                   .IsRequired();

            builder.Property(d => d.LicenseNumber)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.HasIndex(d => d.LicenseNumber)
                   .IsUnique()
                   .HasDatabaseName("IX_Doctors_LicenseNumber");

            builder.HasIndex(d => d.UserId)
                   .IsUnique()
                   .HasDatabaseName("IX_Doctors_UserId");

            builder.Property(d => d.FullName)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(d => d.Qualification)
                   .HasMaxLength(150);

            builder.Property(d => d.ExperienceYears);

            builder.Property(d => d.IsActive)
                   .IsRequired()
                   .HasDefaultValue(true);

            builder.Property(d => d.IsDeleted)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.Property(d => d.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("SYSDATETIME()");

            // Global query filter for soft delete
            builder.HasQueryFilter(d => !d.IsDeleted);

            // Relationships
            builder.HasMany(d => d.DoctorDepartments)
                   .WithOne(dd => dd.Doctor)
                   .HasForeignKey(dd => dd.DoctorId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(d => d.DoctorSchedules)
                   .WithOne(ds => ds.Doctor)
                   .HasForeignKey(ds => ds.DoctorId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(d => d.Appointments)
                   .WithOne(apt => apt.Doctor)
                   .HasForeignKey(apt => apt.DoctorId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
