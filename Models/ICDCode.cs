using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class ICDCode
    {
        [Key]
        public int ICDId { get; set; }

        [Required]
        [StringLength(10)]
        public string Code { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = null!;

        // Navigation properties
        public ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();
    }
}
