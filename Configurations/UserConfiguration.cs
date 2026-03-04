using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.UserId);

            builder.Property(u => u.UserId)
                   .ValueGeneratedOnAdd();

            builder.Property(u => u.Email)
                   .IsRequired()
                   .HasMaxLength(256);

            builder.HasIndex(u => u.Email)
                   .IsUnique()
                   .HasDatabaseName("IX_Users_Email");

            builder.Property(u => u.PasswordHash)
                   .IsRequired()
                   .HasMaxLength(512);

            builder.Property(u => u.IsActive)
                   .IsRequired()
                   .HasDefaultValue(true);

            builder.Property(u => u.IsDeleted)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.Property(u => u.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("SYSDATETIME()");

            builder.Property(u => u.UpdatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("SYSDATETIME()");

            // Global query filter for soft delete
            builder.HasQueryFilter(u => !u.IsDeleted);

            // Relationships
            builder.HasMany(u => u.UserRoles)
                   .WithOne(ur => ur.User)
                   .HasForeignKey(ur => ur.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(u => u.Patient)
                   .WithOne(p => p.User)
                   .HasForeignKey<Patient>(p => p.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(u => u.Doctor)
                   .WithOne(d => d.User)
                   .HasForeignKey<Doctor>(d => d.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.AuditLogs)
                   .WithOne(a => a.User)
                   .HasForeignKey(a => a.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
