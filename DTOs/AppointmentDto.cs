using System.ComponentModel.DataAnnotations;

namespace Axivora.DTOs
{
    public class AppointmentDto
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = null!;
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = null!;
        public int? SlotId { get; set; }
        public DateTime AppointmentStart { get; set; }
        public DateTime AppointmentEnd { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = null!;
    }

    public class CreateAppointmentDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid slotId is required.")]
        public int SlotId { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }
    }

    public class UpdateAppointmentDto
    {
        public string? Reason { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid StatusId is required.")]
        public int? StatusId { get; set; }
    }

    public class UpdateAppointmentStatusDto
    {
        [Required]
        public string Status { get; set; } = null!;
    }

    public class RescheduleAppointmentDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid newSlotId is required.")]
        public int NewSlotId { get; set; }
    }
}
