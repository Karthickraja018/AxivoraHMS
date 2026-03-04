using System;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class PatientAllergy
    {
        public int AllergyId { get; set; }

        public int PatientId { get; set; }

        [Required(ErrorMessage = "Allergen name is required")]
        public string AllergenName { get; set; } = null!;

        public string? Severity { get; set; }

        public string? Reaction { get; set; }

        public DateTime RecordedAt { get; set; }

        // Navigation properties
        public Patient? Patient { get; set; }
    }
}
