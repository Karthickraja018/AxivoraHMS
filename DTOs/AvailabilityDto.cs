using System.ComponentModel.DataAnnotations;

namespace Axivora.DTOs
{
    // Availability Template

    public class CreateAvailabilityTemplateDto : IValidatableObject
    {
        // 0 = Sunday … 6 = Saturday (.NET System.DayOfWeek convention)
        [Range(0, 6, ErrorMessage = "DayOfWeek must be 0 (Sunday) to 6 (Saturday).")]
        public int DayOfWeek { get; set; }

        [Required(ErrorMessage = "StartTime is required.")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "EndTime is required.")]
        public TimeSpan EndTime { get; set; }

        [Range(5, 120, ErrorMessage = "SlotDurationMinutes must be between 5 and 120.")]
        public int SlotDurationMinutes { get; set; } = 15;

        [Required(ErrorMessage = "EffectiveFromDate is required.")]
        public DateOnly EffectiveFromDate { get; set; }

        public DateOnly? EffectiveToDate { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndTime <= StartTime)
                yield return new ValidationResult(
                    "EndTime must be after StartTime.",
                    [nameof(EndTime)]);

            if (EffectiveToDate.HasValue && EffectiveToDate.Value <= EffectiveFromDate)
                yield return new ValidationResult(
                    "EffectiveToDate must be after EffectiveFromDate.",
                    [nameof(EffectiveToDate)]);
        }
    }

    public class AvailabilityTemplateDto
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = null!;
        public int DayOfWeek { get; set; }
        public string DayName { get; set; } = null!;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int SlotDurationMinutes { get; set; }
        public DateOnly EffectiveFromDate { get; set; }
        public DateOnly? EffectiveToDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateAvailabilityTemplateDto
    {
        public bool? IsActive { get; set; }
        public DateOnly? EffectiveToDate { get; set; }
    }

    // Availability Day

    public class AvailabilityDayDto
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public DateOnly Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int SlotDurationMinutes { get; set; }
        public string Status { get; set; } = null!;
        public int? SourceTemplateId { get; set; }
        public int TotalSlots { get; set; }
        public int AvailableSlots { get; set; }
    }

    public class UpdateAvailabilityDayStatusDto
    {
        [Required]
        [RegularExpression("^(Open|Closed|Leave|Holiday)$",
            ErrorMessage = "Status must be Open, Closed, Leave, or Holiday.")]
        public string Status { get; set; } = null!;
    }

    // Appointment Slot

    public class SlotDto
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public int AvailabilityDayId { get; set; }
        public DateTime SlotStart { get; set; }
        public DateTime SlotEnd { get; set; }
        public string Status { get; set; } = null!;
        public int? AppointmentId { get; set; }
    }

    // Booking

    public class BookAppointmentDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid SlotId is required.")]
        public int SlotId { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }
    }

    // Slot-based Reschedule

    public class SlotRescheduleAppointmentDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid NewSlotId is required.")]
        public int NewSlotId { get; set; }
    }

    // Slot Detail

    public class SlotDetailDto
    {
        public int SlotId { get; set; }
        public int DoctorId { get; set; }
        public DateTime SlotStart { get; set; }
        public DateTime SlotEnd { get; set; }
        public string Status { get; set; } = null!;
        public int? AppointmentId { get; set; }
    }

    // Admin Slot Update

    public class UpdateSlotStatusDto
    {
        [Required]
        [RegularExpression("^(Available|Booked|Blocked|Cancelled)$",
            ErrorMessage = "Status must be Available, Booked, Blocked, or Cancelled.")]
        public string Status { get; set; } = null!;
    }

    // Doctor Calendar

    public class DoctorCalendarDayDto
    {
        public DateOnly Date { get; set; }
        public string DayStatus { get; set; } = null!;
        public int TotalSlots { get; set; }
        public int AvailableSlots { get; set; }
        public int BookedSlots { get; set; }
    }

    // Doctor Leave

    public class DoctorLeaveDto : IValidatableObject
    {
        [Required]
        public DateOnly From { get; set; }

        [Required]
        public DateOnly To { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (To < From)
                yield return new ValidationResult(
                    "To date must be on or after From date.",
                    [nameof(To)]);
        }
    }

    // Patient Availability Preview

    public class PatientAvailabilityPreviewDto
    {
        public DateOnly Date { get; set; }
        public int AvailableSlots { get; set; }
    }
}
