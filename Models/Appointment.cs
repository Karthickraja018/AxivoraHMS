using System;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }

        public int PatientId { get; set; }

        public int DoctorId { get; set; }

        public int StatusId { get; set; }

        [Required(ErrorMessage = "Appointment start time is required")]
        [DataType(DataType.DateTime)]
        public DateTime AppointmentStart { get; set; }

        [Required(ErrorMessage = "Appointment end time is required")]
        [DataType(DataType.DateTime)]
        public DateTime AppointmentEnd { get; set; }

        public string? Reason { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public Patient? Patient { get; set; }
        public Doctor? Doctor { get; set; }
        public AppointmentStatus? Status { get; set; }
        public Consultation? Consultation { get; set; }
    }
}
