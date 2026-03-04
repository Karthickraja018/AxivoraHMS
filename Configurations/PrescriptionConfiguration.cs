using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
    {
        public void Configure(EntityTypeBuilder<Prescription> builder)
        {
            builder.ToTable("Prescriptions");

            builder.HasKey(p => p.PrescriptionId);

            builder.Property(p => p.PrescriptionId)
                   .ValueGeneratedOnAdd();

            builder.Property(p => p.ConsultationId)
                   .IsRequired();

            builder.Property(p => p.MedicineId)
                   .IsRequired();

            builder.Property(p => p.Dosage)
                   .HasMaxLength(50);

            builder.Property(p => p.Frequency)
                   .HasMaxLength(50);

            builder.Property(p => p.Route)
                   .HasMaxLength(50);

            builder.Property(p => p.DurationDays);

            builder.Property(p => p.Instructions)
                   .HasMaxLength(200);

            // Indexes for quick lookup
            builder.HasIndex(p => p.ConsultationId)
                   .HasDatabaseName("IX_Prescriptions_ConsultationId");
        }
    }
}
