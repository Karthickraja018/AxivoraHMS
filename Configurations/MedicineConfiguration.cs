using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class MedicineConfiguration : IEntityTypeConfiguration<Medicine>
    {
        public void Configure(EntityTypeBuilder<Medicine> builder)
        {
            builder.ToTable("Medicines");

            builder.HasKey(m => m.MedicineId);

            builder.Property(m => m.MedicineId)
                   .ValueGeneratedOnAdd();

            builder.Property(m => m.MedicineName)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.HasIndex(m => m.MedicineName)
                   .IsUnique()
                   .HasDatabaseName("IX_Medicines_MedicineName");

            // Relationships
            builder.HasMany(m => m.Prescriptions)
                   .WithOne(p => p.Medicine)
                   .HasForeignKey(p => p.MedicineId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
