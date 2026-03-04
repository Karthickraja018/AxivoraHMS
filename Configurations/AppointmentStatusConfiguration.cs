using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class AppointmentStatusConfiguration : IEntityTypeConfiguration<AppointmentStatus>
    {
        public void Configure(EntityTypeBuilder<AppointmentStatus> builder)
        {
            builder.ToTable("AppointmentStatus");

            builder.HasKey(ast => ast.StatusId);

            builder.Property(ast => ast.StatusId)
                   .ValueGeneratedOnAdd();

            builder.Property(ast => ast.StatusName)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.HasIndex(ast => ast.StatusName)
                   .IsUnique()
                   .HasDatabaseName("IX_AppointmentStatus_StatusName");

            // Relationships
            builder.HasMany(ast => ast.Appointments)
                   .WithOne(apt => apt.Status)
                   .HasForeignKey(apt => apt.StatusId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
