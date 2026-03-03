using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string MRN { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = null!;

        [Required]
        public DateOnly DateOfBirth { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(10)]
        public string? BloodGroup { get; set; }

        [StringLength(20)]
        public string? EmergencyContact { get; set; }

        [Required]
        public int AddressId { get; set; }

        public bool IsDeleted { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User? User { get; set; }
        public Address? Address { get; set; }
        public ICollection<PatientAllergy> PatientAllergies { get; set; } = new List<PatientAllergy>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<PatientVital> PatientVitals { get; set; } = new List<PatientVital>();
    }
}
