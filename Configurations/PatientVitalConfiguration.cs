using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
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

            builder.Property(pv => pv.ConsultationId)
                   .IsRequired();

            builder.Property(pv => pv.Temperature_C)
                   .HasPrecision(4, 2);

            builder.Property(pv => pv.Weight_KG)
                   .HasPrecision(5, 2);

            builder.Property(pv => pv.RecordedAt)
                   .IsRequired()
                   .HasDefaultValueSql("SYSDATETIME()");

            // Indexes for quick lookup
            builder.HasIndex(pv => pv.PatientId)
                   .HasDatabaseName("IX_PatientVitals_PatientId");

            builder.HasIndex(pv => pv.ConsultationId)
                   .HasDatabaseName("IX_PatientVitals_ConsultationId");
        }
    }
}
