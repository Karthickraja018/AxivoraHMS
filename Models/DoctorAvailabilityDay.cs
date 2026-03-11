using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    /// <summary>
    /// Represents a concrete calendar working day for a doctor.
    /// Slots are generated from this record based on StartTime, EndTime, and SlotDurationMinutes.
    /// </summary>
    public class DoctorAvailabilityDay
    {
        public int Id { get; set; }

        public int DoctorId { get; set; }

        public DateOnly Date { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Range(5, 120)]
        public int SlotDurationMinutes { get; set; } = 15;

        /// <summary>Open / Closed / Leave / Holiday</summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = AvailabilityDayStatus.Open;

        /// <summary>Optional reference to the template that generated this day.</summary>
        public int? SourceTemplateId { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public Doctor? Doctor { get; set; }
        public DoctorAvailabilityTemplate? SourceTemplate { get; set; }
        public ICollection<AppointmentSlot> Slots { get; set; } = new List<AppointmentSlot>();
    }

    public static class AvailabilityDayStatus
    {
        public const string Open    = "Open";
        public const string Closed  = "Closed";
        public const string Leave   = "Leave";
        public const string Holiday = "Holiday";
    }
}
