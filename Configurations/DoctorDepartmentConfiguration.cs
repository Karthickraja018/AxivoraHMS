using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class DoctorDepartmentConfiguration : IEntityTypeConfiguration<DoctorDepartment>
    {
        public void Configure(EntityTypeBuilder<DoctorDepartment> builder)
        {
            builder.ToTable("DoctorDepartments");

            builder.HasKey(dd => dd.Id);

            builder.Property(dd => dd.Id)
                   .ValueGeneratedOnAdd();

            builder.Property(dd => dd.DoctorId)
                   .IsRequired();

            builder.Property(dd => dd.DepartmentId)
                   .IsRequired();

            builder.HasIndex(dd => new { dd.DoctorId, dd.DepartmentId })
                   .IsUnique()
                   .HasDatabaseName("UQ_DoctorDepartment");
        }
    }
}
