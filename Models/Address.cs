using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class Address
    {
        [Key]
        public int AddressId { get; set; }

        [Required]
        [StringLength(200)]
        public string AddressLine1 { get; set; } = null!;

        [StringLength(200)]
        public string? AddressLine2 { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string State { get; set; } = null!;

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [Required]
        [StringLength(100)]
        public string Country { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Patient> Patients { get; set; } = new List<Patient>();
        public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
    }
}
