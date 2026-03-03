using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class Consultation
    {
        [Key]
        public int ConsultationId { get; set; }

        [Required]
        public int AppointmentId { get; set; }

        public int? ICDId { get; set; }

        [StringLength(1000)]
        public string? ChiefComplaint { get; set; }

        [StringLength(1000)]
        public string? Examination { get; set; }

        [StringLength(500)]
        public string? DiagnosisNotes { get; set; }

        [StringLength(1000)]
        public string? TreatmentPlan { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Appointment? Appointment { get; set; }
        public ICDCode? ICDCode { get; set; }
        public ICollection<PatientVital> PatientVitals { get; set; } = new List<PatientVital>();
        public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        public ICollection<OrderedTest> OrderedTests { get; set; } = new List<OrderedTest>();
    }
}
