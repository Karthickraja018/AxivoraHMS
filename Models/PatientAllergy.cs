using System;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class PatientAllergy
    {
        [Key]
        public int AllergyId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        [StringLength(100)]
        public string AllergenName { get; set; } = null!;

        [StringLength(50)]
        public string? Severity { get; set; }

        [StringLength(200)]
        public string? Reaction { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Patient? Patient { get; set; }
    }
}
