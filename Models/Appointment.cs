using System;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public int StatusId { get; set; }

        [Required]
        public DateTime AppointmentStart { get; set; }

        [Required]
        public DateTime AppointmentEnd { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Patient? Patient { get; set; }
        public Doctor? Doctor { get; set; }
        public AppointmentStatus? Status { get; set; }
        public Consultation? Consultation { get; set; }
    }
}
