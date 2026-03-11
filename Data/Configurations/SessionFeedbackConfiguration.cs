using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Axivora.Models;

namespace Axivora.Data.Configurations
{
    public class SessionFeedbackConfiguration : IEntityTypeConfiguration<SessionFeedback>
    {
        public void Configure(EntityTypeBuilder<SessionFeedback> builder)
        {
            builder.ToTable("SessionFeedbacks");

            builder.HasKey(f => f.FeedbackId);

            builder.Property(f => f.FeedbackId)
                   .ValueGeneratedOnAdd();

            builder.Property(f => f.ConsultationId)
                   .IsRequired();

            builder.Property(f => f.PatientId)
                   .IsRequired();

            // Rating: 1 (Very Poor) … 5 (Excellent)
            builder.Property(f => f.Rating)
                   .IsRequired();

            builder.HasCheckConstraint(
                "CHK_SessionFeedbacks_Rating",
                "[Rating] >= 1 AND [Rating] <= 5");

            builder.Property(f => f.Comment)
                   .HasMaxLength(1000);

            builder.Property(f => f.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("SYSDATETIME()");

            builder.Property(f => f.IsEdited)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.Property(f => f.UpdatedAt);

            // One consultation ? at most one feedback
            builder.HasIndex(f => f.ConsultationId)
                   .IsUnique()
                   .HasDatabaseName("UQ_SessionFeedbacks_ConsultationId");

            builder.HasIndex(f => f.PatientId)
                   .HasDatabaseName("IX_SessionFeedbacks_PatientId");

            // Relationships
            builder.HasOne(f => f.Consultation)
                   .WithOne(c => c.SessionFeedback)
                   .HasForeignKey<SessionFeedback>(f => f.ConsultationId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(f => f.Patient)
                   .WithMany()
                   .HasForeignKey(f => f.PatientId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
