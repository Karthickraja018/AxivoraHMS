using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class DoctorAvailabilityTemplateConfiguration : IEntityTypeConfiguration<DoctorAvailabilityTemplate>
    {
        public void Configure(EntityTypeBuilder<DoctorAvailabilityTemplate> builder)
        {
            builder.ToTable("DoctorAvailabilityTemplates");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id).ValueGeneratedOnAdd();

            builder.Property(t => t.DoctorId).IsRequired();

            builder.Property(t => t.DayOfWeek).IsRequired();
            builder.HasCheckConstraint("CHK_AvailabilityTemplate_DayOfWeek",
                "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6");

            builder.Property(t => t.StartTime).IsRequired();
            builder.Property(t => t.EndTime).IsRequired();
            builder.HasCheckConstraint("CHK_AvailabilityTemplate_Times",
                "[EndTime] > [StartTime]");

            builder.Property(t => t.SlotDurationMinutes)
                   .IsRequired()
                   .HasDefaultValue(15);
            builder.HasCheckConstraint("CHK_AvailabilityTemplate_SlotDuration",
                "[SlotDurationMinutes] >= 5 AND [SlotDurationMinutes] <= 120");

            builder.Property(t => t.EffectiveFromDate).IsRequired();
            builder.Property(t => t.EffectiveToDate);

            builder.Property(t => t.IsActive).IsRequired().HasDefaultValue(true);

            builder.Property(t => t.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("SYSDATETIME()");

            // Indexes
            builder.HasIndex(t => new { t.DoctorId, t.DayOfWeek })
                   .HasDatabaseName("IX_AvailabilityTemplates_DoctorId_DayOfWeek");

            builder.HasIndex(t => new { t.DoctorId, t.IsActive })
                   .HasDatabaseName("IX_AvailabilityTemplates_DoctorId_IsActive");
        }
    }
}
