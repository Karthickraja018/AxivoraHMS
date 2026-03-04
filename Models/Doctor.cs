using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class Doctor
    {
        public int DoctorId { get; set; }

        public int UserId { get; set; }

        [Required(ErrorMessage = "License number is required")]
        public string LicenseNumber { get; set; } = null!;

        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; } = null!;

        public string? Qualification { get; set; }

        [Range(0, 100, ErrorMessage = "Experience years must be between 0 and 100")]
        public int? ExperienceYears { get; set; }

        public int? AddressId { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public Address? Address { get; set; }
        public ICollection<DoctorDepartment> DoctorDepartments { get; set; } = new List<DoctorDepartment>();
        public ICollection<DoctorSchedule> DoctorSchedules { get; set; } = new List<DoctorSchedule>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
