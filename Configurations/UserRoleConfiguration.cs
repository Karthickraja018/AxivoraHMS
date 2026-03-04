using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable("UserRoles");

            builder.HasKey(ur => ur.UserRoleId);

            builder.Property(ur => ur.UserRoleId)
                   .ValueGeneratedOnAdd();

            builder.Property(ur => ur.UserId)
                   .IsRequired();

            builder.Property(ur => ur.RoleId)
                   .IsRequired();

            builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
                   .IsUnique()
                   .HasDatabaseName("UQ_UserRole");
        }
    }
}
