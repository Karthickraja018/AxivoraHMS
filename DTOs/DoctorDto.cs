using System.ComponentModel.DataAnnotations;

namespace Axivora.DTOs
{
    public class DoctorDto
    {
        public int DoctorId { get; set; }
        public string LicenseNumber { get; set; }
        public string FullName { get; set; }
        public string Qualification { get; set; }
        public int? ExperienceYears { get; set; }
        public bool IsActive { get; set; }
        public AddressDto Address { get; set; }
        public List<DepartmentDto> Departments { get; set; }
    }

    /// <summary>
    /// Admin use only - create doctor with user account in one transaction
    /// </summary>
    public class CreateDoctorDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string LicenseNumber { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = null!;

        [StringLength(150)]
        public string? Qualification { get; set; }

        [Range(0, 100)]
        public int? ExperienceYears { get; set; }

        public CreateAddressDto? Address { get; set; }

        [Required]
        public List<int> DepartmentIds { get; set; } = new List<int>();
    }

    /// <summary>
    /// Admin-only: create login (User + Doctor role). Clinician completes profile after first sign-in.
    /// </summary>
    public class InviteDoctorDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = null!;

        /// <summary>Optional display name used in the invitation email.</summary>
        [StringLength(150)]
        public string? DisplayName { get; set; }
    }

    /// <summary>
    /// Doctor self-service: create clinician profile for the authenticated user (no new User row).
    /// </summary>
    public class CompleteDoctorProfileDto
    {
        [Required]
        [StringLength(100)]
        public string LicenseNumber { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = null!;

        [StringLength(150)]
        public string? Qualification { get; set; }

        [Range(0, 100)]
        public int? ExperienceYears { get; set; }

        public CreateAddressDto? Address { get; set; }

        [Required]
        public List<int> DepartmentIds { get; set; } = new List<int>();
    }

    public class UpdateDoctorDto
    {
        [StringLength(150)]
        public string? FullName { get; set; }

        [StringLength(150)]
        public string? Qualification { get; set; }

        [Range(0, 100)]
        public int? ExperienceYears { get; set; }

        public int? AddressId { get; set; }

        public bool IsActive { get; set; }
    }
}
