using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class LabTestConfiguration : IEntityTypeConfiguration<LabTest>
    {
        public void Configure(EntityTypeBuilder<LabTest> builder)
        {
            builder.ToTable("LabTests");

            builder.HasKey(lt => lt.LabTestId);

            builder.Property(lt => lt.LabTestId)
                   .ValueGeneratedOnAdd();

            builder.Property(lt => lt.TestName)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.HasIndex(lt => lt.TestName)
                   .IsUnique()
                   .HasDatabaseName("IX_LabTests_TestName");

            // Relationships
            builder.HasMany(lt => lt.OrderedTests)
                   .WithOne(ot => ot.LabTest)
                   .HasForeignKey(ot => ot.LabTestId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
