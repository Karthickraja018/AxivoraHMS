using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class Medicine
    {
        public int MedicineId { get; set; }

        [Required(ErrorMessage = "Medicine name is required")]
        public string MedicineName { get; set; } = null!;

        // Navigation properties
        public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }
}
