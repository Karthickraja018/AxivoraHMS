using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    /// <summary>
    /// Defines a recurring weekly working pattern for a doctor.
    /// Does NOT generate slots directly — slots are generated from DoctorAvailabilityDay records.
    /// </summary>
    public class DoctorAvailabilityTemplate
    {
        public int Id { get; set; }

        public int DoctorId { get; set; }

        // 0 = Sunday ... 6 = Saturday (.NET System.DayOfWeek convention)
        [Range(0, 6)]
        public int DayOfWeek { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Range(5, 120)]
        public int SlotDurationMinutes { get; set; } = 15;

        public DateOnly EffectiveFromDate { get; set; }

        public DateOnly? EffectiveToDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public Doctor? Doctor { get; set; }
        public ICollection<DoctorAvailabilityDay> AvailabilityDays { get; set; } = new List<DoctorAvailabilityDay>();
    }
}
