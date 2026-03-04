using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
    {
        public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
        {
            builder.ToTable("DoctorSchedules");

            builder.HasKey(ds => ds.ScheduleId);

            builder.Property(ds => ds.ScheduleId)
                   .ValueGeneratedOnAdd();

            builder.Property(ds => ds.DoctorId)
                   .IsRequired();

            builder.Property(ds => ds.DayOfWeek)
                   .IsRequired();

            builder.Property(ds => ds.StartTime)
                   .IsRequired();

            builder.Property(ds => ds.EndTime)
                   .IsRequired();

            builder.Property(ds => ds.SlotDurationMinutes)
                   .IsRequired()
                   .HasDefaultValue(15);

            builder.Property(ds => ds.IsActive)
                   .IsRequired()
                   .HasDefaultValue(true);

            // Index for quick lookup of doctor schedules
            builder.HasIndex(ds => new { ds.DoctorId, ds.DayOfWeek })
                   .HasDatabaseName("IX_DoctorSchedules_DoctorId_DayOfWeek");
        }
    }
}
