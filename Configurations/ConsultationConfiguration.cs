using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Configurations
{
    public class ConsultationConfiguration : IEntityTypeConfiguration<Consultation>
    {
        public void Configure(EntityTypeBuilder<Consultation> builder)
        {
            builder.ToTable("Consultations");

            builder.HasKey(c => c.ConsultationId);

            builder.Property(c => c.ConsultationId)
                   .ValueGeneratedOnAdd();

            builder.Property(c => c.AppointmentId)
                   .IsRequired();

            builder.HasIndex(c => c.AppointmentId)
                   .IsUnique()
                   .HasDatabaseName("IX_Consultations_AppointmentId");

            builder.Property(c => c.ChiefComplaint)
                   .HasMaxLength(1000);

            builder.Property(c => c.Examination)
                   .HasMaxLength(1000);

            builder.Property(c => c.DiagnosisNotes)
                   .HasMaxLength(500);

            builder.Property(c => c.TreatmentPlan)
                   .HasMaxLength(1000);

            builder.Property(c => c.Notes);

            builder.Property(c => c.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("SYSDATETIME()");

            // Relationships
            builder.HasMany(c => c.PatientVitals)
                   .WithOne(pv => pv.Consultation)
                   .HasForeignKey(pv => pv.ConsultationId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Prescriptions)
                   .WithOne(p => p.Consultation)
                   .HasForeignKey(p => p.ConsultationId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.OrderedTests)
                   .WithOne(ot => ot.Consultation)
                   .HasForeignKey(ot => ot.ConsultationId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
