using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class AddressConfiguration : IEntityTypeConfiguration<Address>
    {
        public void Configure(EntityTypeBuilder<Address> builder)
        {
            builder.ToTable("Addresses");

            builder.HasKey(a => a.AddressId);

            builder.Property(a => a.AddressId)
                   .ValueGeneratedOnAdd();

            builder.Property(a => a.AddressLine1)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(a => a.AddressLine2)
                   .HasMaxLength(200);

            builder.Property(a => a.City)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(a => a.State)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(a => a.PostalCode)
                   .HasMaxLength(20);

            builder.Property(a => a.Country)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(a => a.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("SYSDATETIME()");

            // Relationships
            builder.HasMany(a => a.Patients)
                   .WithOne(p => p.Address)
                   .HasForeignKey(p => p.AddressId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(a => a.Doctors)
                   .WithOne(d => d.Address)
                   .HasForeignKey(d => d.AddressId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
