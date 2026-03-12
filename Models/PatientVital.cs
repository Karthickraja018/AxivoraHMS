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

        public decimal? Height { get; set; }

        public decimal? Weight { get; set; }

        [StringLength(20)]
        public string? BloodPressure { get; set; }

        public int? HeartRate { get; set; }

        public decimal? Temperature { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Patient? Patient { get; set; }
    }
}
