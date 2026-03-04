using System;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class DoctorSchedule
    {
        public int ScheduleId { get; set; }

        public int DoctorId { get; set; }

        [Range(0, 6, ErrorMessage = "Day of week must be between 0 (Sunday) and 6 (Saturday)")]
        public int DayOfWeek { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End time is required")]
        public TimeSpan EndTime { get; set; }

        [Range(5, 120, ErrorMessage = "Slot duration must be between 5 and 120 minutes")]
        public int SlotDurationMinutes { get; set; }

        public bool IsActive { get; set; }

        // Navigation properties
        public Doctor? Doctor { get; set; }
    }
}
