using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class Prescription
    {
        [Key]
        public int PrescriptionId { get; set; }

        [Required]
        public int ConsultationId { get; set; }

        [Required]
        public int MedicineId { get; set; }

        [StringLength(50)]
        public string? Dosage { get; set; }

        [StringLength(50)]
        public string? Frequency { get; set; }

        [StringLength(50)]
        public string? Route { get; set; }

        public int? DurationDays { get; set; }

        [StringLength(200)]
        public string? Instructions { get; set; }

        // Navigation properties
        public Consultation? Consultation { get; set; }
        public Medicine? Medicine { get; set; }
    }
}
