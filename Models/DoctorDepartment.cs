using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class DoctorDepartment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        // Navigation properties
        public Doctor? Doctor { get; set; }
        public Department? Department { get; set; }
    }
}
