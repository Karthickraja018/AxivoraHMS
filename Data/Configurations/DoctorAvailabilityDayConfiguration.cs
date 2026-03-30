using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Data.Configurations
{
    public class DoctorAvailabilityDayConfiguration : IEntityTypeConfiguration<DoctorAvailabilityDay>
    {
        public void Configure(EntityTypeBuilder<DoctorAvailabilityDay> builder)
        {
            builder.ToTable("DoctorAvailabilityDays");

            builder.HasKey(d => d.Id);
            builder.Property(d => d.Id).ValueGeneratedOnAdd();

            builder.Property(d => d.DoctorId).IsRequired();
            builder.Property(d => d.Date).IsRequired();
            builder.Property(d => d.StartTime).IsRequired();
            builder.Property(d => d.EndTime).IsRequired();
            builder.HasCheckConstraint("CHK_AvailabilityDay_Times",
                "[EndTime] > [StartTime]");

            builder.Property(d => d.SlotDurationMinutes)
                   .IsRequired()
                   .HasDefaultValue(15);

            builder.Property(d => d.Status)
                   .IsRequired()
                   .HasMaxLength(20)
                   .HasDefaultValue("Open");

            builder.Property(d => d.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("SYSDATETIME()");

            // Index for efficient calendar lookups
            builder.HasIndex(d => new { d.DoctorId, d.Date })
                   .HasDatabaseName("IX_AvailabilityDays_DoctorId_Date");

            // Index for efficient slot generation queries
            builder.HasIndex(d => new { d.DoctorId, d.Date, d.Status })
                   .HasDatabaseName("IX_AvailabilityDays_DoctorId_Date_Status");

            builder.HasOne(d => d.Doctor)
                   .WithMany(d => d.AvailabilityDays)
                   .HasForeignKey(d => d.DoctorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.SourceTemplate)
                   .WithMany(t => t.AvailabilityDays)
                   .HasForeignKey(d => d.SourceTemplateId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
