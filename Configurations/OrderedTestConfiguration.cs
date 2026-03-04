using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class OrderedTestConfiguration : IEntityTypeConfiguration<OrderedTest>
    {
        public void Configure(EntityTypeBuilder<OrderedTest> builder)
        {
            builder.ToTable("OrderedTests");

            builder.HasKey(ot => ot.OrderedTestId);

            builder.Property(ot => ot.OrderedTestId)
                   .ValueGeneratedOnAdd();

            builder.Property(ot => ot.ConsultationId)
                   .IsRequired();

            builder.Property(ot => ot.LabTestId)
                   .IsRequired();

            builder.Property(ot => ot.Status)
                   .IsRequired()
                   .HasMaxLength(50)
                   .HasDefaultValue("Pending");

            builder.Property(ot => ot.Result);

            builder.Property(ot => ot.ResultDate);

            // Indexes for quick lookup
            builder.HasIndex(ot => ot.ConsultationId)
                   .HasDatabaseName("IX_OrderedTests_ConsultationId");

            builder.HasIndex(ot => ot.Status)
                   .HasDatabaseName("IX_OrderedTests_Status");
        }
    }
}
