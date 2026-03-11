using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Data.Configurations
{
    public class AppointmentReportViewConfiguration : IEntityTypeConfiguration<AppointmentReportView>
    {
        public void Configure(EntityTypeBuilder<AppointmentReportView> builder)
        {
            builder.HasNoKey();
            builder.ToView("vw_AppointmentReport");

            builder.Property(v => v.Reason).HasMaxLength(500);
            builder.Property(v => v.StatusName).IsRequired().HasMaxLength(50);
            builder.Property(v => v.PatientName).IsRequired().HasMaxLength(150);
            builder.Property(v => v.PatientPhone).HasMaxLength(20);
            builder.Property(v => v.MRN).IsRequired().HasMaxLength(50);
            builder.Property(v => v.DoctorName).IsRequired().HasMaxLength(150);
            builder.Property(v => v.DepartmentName).HasMaxLength(100);
        }
    }
}
