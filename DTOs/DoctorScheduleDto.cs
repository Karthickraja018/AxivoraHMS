using System.ComponentModel.DataAnnotations;

namespace Axivora.DTOs
{
    public class DoctorScheduleDto
    {
        public int ScheduleId { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = null!;
        public int DayOfWeek { get; set; }
        public string DayName { get; set; } = null!;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int SlotDurationMinutes { get; set; }
        public bool IsActive { get; set; }
        public List<string> GeneratedSlots { get; set; } = new();
    }

    public class CreateScheduleDto
    {
        [Range(0, 6, ErrorMessage = "DayOfWeek must be 0 (Sunday) to 6 (Saturday).")]
        public int DayOfWeek { get; set; }

        [Required(ErrorMessage = "StartTime is required.")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "EndTime is required.")]
        public TimeSpan EndTime { get; set; }

        [Range(5, 120, ErrorMessage = "SlotDurationMinutes must be between 5 and 120.")]
        public int SlotDurationMinutes { get; set; } = 15;
    }

    public class UpdateScheduleDto
    {
        [Range(0, 6, ErrorMessage = "DayOfWeek must be 0 (Sunday) to 6 (Saturday).")]
        public int? DayOfWeek { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        [Range(5, 120, ErrorMessage = "SlotDurationMinutes must be between 5 and 120.")]
        public int? SlotDurationMinutes { get; set; }

        public bool? IsActive { get; set; }
    }
}
