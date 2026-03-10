using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

            builder.HasKey(rt => rt.Id);

            builder.Property(rt => rt.Id)
                   .ValueGeneratedOnAdd();

            builder.Property(rt => rt.Token)
                   .IsRequired()
                   .HasMaxLength(512);

            builder.HasIndex(rt => rt.Token)
                   .IsUnique()
                   .HasDatabaseName("IX_RefreshTokens_Token");

            builder.Property(rt => rt.ExpiresAt)
                   .IsRequired();

            builder.Property(rt => rt.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("SYSDATETIME()");

            builder.Property(rt => rt.IsRevoked)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.Property(rt => rt.RevokedAt)
                   .IsRequired(false);

            builder.HasOne(rt => rt.User)
                   .WithMany(u => u.RefreshTokens)
                   .HasForeignKey(rt => rt.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(rt => rt.UserId)
                   .HasDatabaseName("IX_RefreshTokens_UserId");
        }
    }
}
