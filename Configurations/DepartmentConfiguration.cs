using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> builder)
        {
            builder.ToTable("Departments");

            builder.HasKey(d => d.DepartmentId);

            builder.Property(d => d.DepartmentId)
                   .ValueGeneratedOnAdd();

            builder.Property(d => d.DepartmentName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.HasIndex(d => d.DepartmentName)
                   .IsUnique()
                   .HasDatabaseName("IX_Departments_DepartmentName");

            // Relationships
            builder.HasMany(d => d.DoctorDepartments)
                   .WithOne(dd => dd.Department)
                   .HasForeignKey(dd => dd.DepartmentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
