using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class AppointmentStatus
    {
        [Key]
        public int StatusId { get; set; }

        [Required]
        [StringLength(50)]
        public string StatusName { get; set; } = null!;

        // Navigation properties
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
