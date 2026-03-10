using System;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class DoctorSchedule
    {
        public int ScheduleId { get; set; }

        public int DoctorId { get; set; }

        // DayOfWeek follows the .NET System.DayOfWeek enum convention:
        //   0 = Sunday, 1 = Monday, 2 = Tuesday, 3 = Wednesday,
        //   4 = Thursday, 5 = Friday, 6 = Saturday.
        // This is the single canonical source for day numbering in this codebase.
        // Never compare against SQL Server DATEPART(weekday, ...) directly — that
        // function returns 1-based values under the default DATEFIRST 7 setting.
        // Always use (int)DateTime.DayOfWeek when matching against this column.
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
