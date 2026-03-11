using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class DoctorWorkloadReportViewConfiguration : IEntityTypeConfiguration<DoctorWorkloadReportView>
    {
        public void Configure(EntityTypeBuilder<DoctorWorkloadReportView> builder)
        {
            builder.HasNoKey();
            builder.ToView("vw_DoctorWorkloadReport");

            builder.Property(v => v.DoctorName).IsRequired().HasMaxLength(150);
            builder.Property(v => v.Qualification).HasMaxLength(150);
            builder.Property(v => v.DepartmentName).HasMaxLength(100);
        }
    }
}
