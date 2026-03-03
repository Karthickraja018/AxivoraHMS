using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class Medicine
    {
        [Key]
        public int MedicineId { get; set; }

        [Required]
        [StringLength(150)]
        public string MedicineName { get; set; } = null!;

        // Navigation properties
        public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }
}
