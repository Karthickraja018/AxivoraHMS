using System.ComponentModel.DataAnnotations;

namespace Axivora.DTOs
{
    public class PatientVitalDto
    {
        public int VitalId { get; set; }
        public int PatientId { get; set; }
        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }
        public string? BloodPressure { get; set; }
        public int? HeartRate { get; set; }
        public decimal? Temperature { get; set; }
        public DateTime RecordedAt { get; set; }
    }

    public class CreatePatientVitalDto
    {
        [Range(0.01, 300, ErrorMessage = "Height must be between 0.01 and 300 cm")]
        public decimal? Height { get; set; }

        [Range(0.01, 700, ErrorMessage = "Weight must be between 0.01 and 700 kg")]
        public decimal? Weight { get; set; }

        [StringLength(20, ErrorMessage = "BloodPressure cannot exceed 20 characters")]
        public string? BloodPressure { get; set; }

        [Range(1, 300, ErrorMessage = "HeartRate must be between 1 and 300 bpm")]
        public int? HeartRate { get; set; }

        [Range(30, 45, ErrorMessage = "Temperature must be between 30 and 45 °C")]
        public decimal? Temperature { get; set; }
    }

    public class UpdatePatientVitalDto
    {
        [Range(0.01, 300, ErrorMessage = "Height must be between 0.01 and 300 cm")]
        public decimal? Height { get; set; }

        [Range(0.01, 700, ErrorMessage = "Weight must be between 0.01 and 700 kg")]
        public decimal? Weight { get; set; }

        [StringLength(20, ErrorMessage = "BloodPressure cannot exceed 20 characters")]
        public string? BloodPressure { get; set; }

        [Range(1, 300, ErrorMessage = "HeartRate must be between 1 and 300 bpm")]
        public int? HeartRate { get; set; }

        [Range(30, 45, ErrorMessage = "Temperature must be between 30 and 45 °C")]
        public decimal? Temperature { get; set; }
    }
}
