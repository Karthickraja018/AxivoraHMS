using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class PatientAllergyConfiguration : IEntityTypeConfiguration<PatientAllergy>
    {
        public void Configure(EntityTypeBuilder<PatientAllergy> builder)
        {
            builder.ToTable("PatientAllergies");

            builder.HasKey(pa => pa.AllergyId);

            builder.Property(pa => pa.AllergyId)
                   .ValueGeneratedOnAdd();

            builder.Property(pa => pa.PatientId)
                   .IsRequired();

            builder.Property(pa => pa.AllergenName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(pa => pa.Severity)
                   .HasMaxLength(50);

            builder.Property(pa => pa.Reaction)
                   .HasMaxLength(200);

            builder.Property(pa => pa.RecordedAt)
                   .IsRequired()
                   .HasDefaultValueSql("SYSDATETIME()");

            // Index for quick lookup
            builder.HasIndex(pa => pa.PatientId)
                   .HasDatabaseName("IX_PatientAllergies_PatientId");
        }
    }
}
