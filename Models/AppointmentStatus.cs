using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class AppointmentStatus
    {
        public int StatusId { get; set; }

        [Required(ErrorMessage = "Status name is required")]
        public string StatusName { get; set; } = null!;

        // Navigation properties
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
