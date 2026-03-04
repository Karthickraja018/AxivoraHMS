using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class Address
    {
        public int AddressId { get; set; }

        [Required(ErrorMessage = "Address Line 1 is required")]
        public string AddressLine1 { get; set; } = null!;

        public string? AddressLine2 { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; } = null!;

        [Required(ErrorMessage = "State is required")]
        public string State { get; set; } = null!;

        public string? PostalCode { get; set; }

        [Required(ErrorMessage = "Country is required")]
        public string Country { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public ICollection<Patient> Patients { get; set; } = new List<Patient>();
        public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
    }
}
