using System;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class DoctorSchedule
    {
        [Key]
        public int ScheduleId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        public int DayOfWeek { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public int SlotDurationMinutes { get; set; } = 15;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public Doctor? Doctor { get; set; }
    }
}
