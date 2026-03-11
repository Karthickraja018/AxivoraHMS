using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Data.Configurations
{
    public class AppointmentSlotConfiguration : IEntityTypeConfiguration<AppointmentSlot>
    {
        public void Configure(EntityTypeBuilder<AppointmentSlot> builder)
        {
            builder.ToTable("AppointmentSlots");

            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedOnAdd();

            builder.Property(s => s.DoctorId).IsRequired();
            builder.Property(s => s.AvailabilityDayId).IsRequired();
            builder.Property(s => s.SlotStart).IsRequired();
            builder.Property(s => s.SlotEnd).IsRequired();
            builder.HasCheckConstraint("CHK_AppointmentSlot_Times",
                "[SlotEnd] > [SlotStart]");

            builder.Property(s => s.Status)
                   .IsRequired()
                   .HasMaxLength(20)
                   .HasDefaultValue("Available");

            // Optimistic concurrency token
            builder.Property(s => s.RowVersion)
                   .IsRowVersion()
                   .IsRequired();

            // AppointmentId must be unique — one slot can only be linked to one appointment
            builder.HasIndex(s => s.AppointmentId)
                   .IsUnique()
                   .HasFilter("[AppointmentId] IS NOT NULL")
                   .HasDatabaseName("UQ_AppointmentSlots_AppointmentId");

            // Indexes for availability queries
            builder.HasIndex(s => new { s.DoctorId, s.SlotStart })
                   .HasDatabaseName("IX_AppointmentSlots_DoctorId_SlotStart");

            builder.HasIndex(s => s.Status)
                   .HasDatabaseName("IX_AppointmentSlots_Status");

            builder.HasIndex(s => new { s.AvailabilityDayId, s.Status })
                   .HasDatabaseName("IX_AppointmentSlots_AvailabilityDayId_Status");

            // Relationships
            builder.HasOne(s => s.AvailabilityDay)
                   .WithMany(d => d.Slots)
                   .HasForeignKey(s => s.AvailabilityDayId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.Appointment)
                   .WithOne(a => a.Slot)
                   .HasForeignKey<AppointmentSlot>(s => s.AppointmentId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
