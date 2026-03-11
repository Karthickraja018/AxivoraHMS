using System.ComponentModel.DataAnnotations;

namespace Axivora.DTOs
{
    public class AppointmentDto
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public DateTime AppointmentStart { get; set; }
        public DateTime AppointmentEnd { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
    }

    public class CreateAppointmentDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid PatientId is required.")]
        public int PatientId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid DoctorId is required.")]
        public int DoctorId { get; set; }

        [Required]
        public DateTime AppointmentStart { get; set; }

        [Required]
        public DateTime AppointmentEnd { get; set; }

        [StringLength(500)]
        public string Reason { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid StatusId is required.")]
        public int StatusId { get; set; }
    }

    public class UpdateAppointmentDto
    {
        public DateTime? AppointmentStart { get; set; }
        public DateTime? AppointmentEnd { get; set; }
        public string Reason { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid StatusId is required.")]
        public int? StatusId { get; set; }
    }

    public class UpdateAppointmentStatusDto
    {
        [Required]
        public string Status { get; set; } = null!;
    }

    /// <summary>Payload for rescheduling an existing appointment to a new time window.</summary>
    public class RescheduleAppointmentDto : IValidatableObject
    {
        /// <summary>New start date and time for the appointment (UTC).</summary>
        [Required(ErrorMessage = "AppointmentStart is required.")]
        public DateTime AppointmentStart { get; set; }

        /// <summary>New end date and time for the appointment (UTC).</summary>
        [Required(ErrorMessage = "AppointmentEnd is required.")]
        public DateTime AppointmentEnd { get; set; }

        /// <inheritdoc />
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (AppointmentEnd <= AppointmentStart)
                yield return new ValidationResult(
                    "AppointmentEnd must be after AppointmentStart.",
                    [nameof(AppointmentEnd)]);
        }
    }
}
