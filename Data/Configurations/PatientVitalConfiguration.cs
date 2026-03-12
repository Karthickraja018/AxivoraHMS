using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Data.Configurations
{
    public class PatientVitalConfiguration : IEntityTypeConfiguration<PatientVital>
    {
        public void Configure(EntityTypeBuilder<PatientVital> builder)
        {
            builder.ToTable("PatientVitals");

            builder.HasKey(pv => pv.VitalId);

            builder.Property(pv => pv.VitalId)
                   .ValueGeneratedOnAdd();

            builder.Property(pv => pv.PatientId)
                   .IsRequired();

            builder.Property(pv => pv.Height)
                   .HasPrecision(5, 2);

            builder.Property(pv => pv.Weight)
                   .HasPrecision(5, 2);

            builder.Property(pv => pv.BloodPressure)
                   .HasMaxLength(20);

            builder.Property(pv => pv.Temperature)
                   .HasPrecision(4, 2);

            builder.Property(pv => pv.RecordedAt)
                   .IsRequired()
                   .HasDefaultValueSql("SYSDATETIME()");

            builder.HasOne(pv => pv.Patient)
                   .WithMany(p => p.PatientVitals)
                   .HasForeignKey(pv => pv.PatientId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(pv => new { pv.PatientId, pv.RecordedAt })
                   .HasDatabaseName("IX_PatientVitals_PatientId_RecordedAt");
        }
    }
}
