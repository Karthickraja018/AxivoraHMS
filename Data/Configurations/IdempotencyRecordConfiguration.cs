using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Data.Configurations
{
    public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
    {
        public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
        {
            builder.ToTable("IdempotencyRecords");

            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).ValueGeneratedOnAdd();

            builder.Property(r => r.IdempotencyKey)
                   .IsRequired()
                   .HasMaxLength(256);

            builder.Property(r => r.RequestHash)
                   .IsRequired()
                   .HasMaxLength(64);

            builder.Property(r => r.ResponsePayload)
                   .IsRequired();

            builder.Property(r => r.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("SYSDATETIME()");

            // Unique index — one stored result per idempotency key
            builder.HasIndex(r => r.IdempotencyKey)
                   .IsUnique()
                   .HasDatabaseName("UQ_IdempotencyRecords_Key");
        }
    }
}
