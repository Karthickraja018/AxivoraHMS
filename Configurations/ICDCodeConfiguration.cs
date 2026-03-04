using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class ICDCodeConfiguration : IEntityTypeConfiguration<ICDCode>
    {
        public void Configure(EntityTypeBuilder<ICDCode> builder)
        {
            builder.ToTable("ICDCodes");

            builder.HasKey(icd => icd.ICDId);

            builder.Property(icd => icd.ICDId)
                   .ValueGeneratedOnAdd();

            builder.Property(icd => icd.Code)
                   .IsRequired()
                   .HasMaxLength(10);

            builder.HasIndex(icd => icd.Code)
                   .IsUnique()
                   .HasDatabaseName("IX_ICDCodes_Code");

            builder.Property(icd => icd.Description)
                   .IsRequired()
                   .HasMaxLength(500);

            // Relationships
            builder.HasMany(icd => icd.Consultations)
                   .WithOne(c => c.ICDCode)
                   .HasForeignKey(c => c.ICDId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
