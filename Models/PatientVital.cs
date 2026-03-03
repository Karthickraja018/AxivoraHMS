using System;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class PatientVital
    {
        [Key]
        public int VitalId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int ConsultationId { get; set; }

        public decimal? Temperature_C { get; set; }

        public int? SystolicBP { get; set; }

        public int? DiastolicBP { get; set; }

        public int? HeartRate_BPM { get; set; }

        public int? SpO2_Percentage { get; set; }

        public decimal? Weight_KG { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Patient? Patient { get; set; }
        public Consultation? Consultation { get; set; }
    }
}
