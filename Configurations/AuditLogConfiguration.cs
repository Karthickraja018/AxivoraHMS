using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");

            builder.HasKey(al => al.AuditId);

            builder.Property(al => al.AuditId)
                   .ValueGeneratedOnAdd();

            builder.Property(al => al.UserId)
                   .IsRequired();

            builder.Property(al => al.Action)
                   .HasMaxLength(100);

            builder.Property(al => al.EntityName)
                   .HasMaxLength(100);

            builder.Property(al => al.EntityId);

            builder.Property(al => al.OldValue);

            builder.Property(al => al.NewValue);

            builder.Property(al => al.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("SYSDATETIME()");

            // Indexes for quick lookup
            builder.HasIndex(al => al.UserId)
                   .HasDatabaseName("IX_AuditLogs_UserId");

            builder.HasIndex(al => new { al.EntityName, al.EntityId })
                   .HasDatabaseName("IX_AuditLogs_EntityName_EntityId");

            builder.HasIndex(al => al.CreatedAt)
                   .HasDatabaseName("IX_AuditLogs_CreatedAt");
        }
    }
}
