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

            // Convention: DayOfWeek is stored using .NET System.DayOfWeek values
            // (0=Sunday, 1=Monday, … 6=Saturday). This matches (int)DateTime.DayOfWeek
            // and must never be compared against SQL Server DATEPART(weekday, …) directly,
            // which returns 1-based values under the default DATEFIRST 7 setting.
            builder.HasCheckConstraint(
                "CHK_DoctorSchedules_DayOfWeek",
                "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6");

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

            builder.HasCheckConstraint(
                "CHK_DoctorSchedule_Times",
                "\"EndTime\" > \"StartTime\"");

            // Index for quick lookup of doctor schedules
            builder.HasIndex(ds => new { ds.DoctorId, ds.DayOfWeek })
                   .HasDatabaseName("IX_DoctorSchedules_DoctorId_DayOfWeek");
        }
    }
}
