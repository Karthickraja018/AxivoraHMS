using System.ComponentModel.DataAnnotations;

namespace Axivora.DTOs
{
    public class PatientDto
    {
        public int PatientId { get; set; }
        public int UserId { get; set; }
        public string MRN { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public DateOnly DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? PhoneNumber { get; set; }
        public string? BloodGroup { get; set; }
        public string? EmergencyContact { get; set; }
        public AddressDto? Address { get; set; }
        public List<PatientAllergyDto>? Allergies { get; set; }
    }

    /// <summary>
    /// Used for completing patient profile after user registration
    /// User must be authenticated to call this
    /// </summary>
    public class CompletePatientProfileDto
    {
        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = null!;

        [Required]
        public DateOnly DateOfBirth { get; set; }

        [Required]
        [StringLength(20)]
        public string Gender { get; set; } = null!;

        [Required]
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = null!;

        [StringLength(10)]
        public string? BloodGroup { get; set; }

        [Phone]
        [StringLength(20)]
        public string? EmergencyContact { get; set; }

        [Required]
        public CreateAddressDto Address { get; set; } = null!;
    }

    /// <summary>
    /// Admin use only - create patient with user account
    /// </summary>
    public class CreatePatientDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = null!;

        [Required]
        public DateOnly DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; } = null!;

        [Phone]
        public string? PhoneNumber { get; set; }

        public string? BloodGroup { get; set; }

        [Phone]
        public string? EmergencyContact { get; set; }

        [Required]
        public CreateAddressDto Address { get; set; } = null!;
    }

    public class UpdatePatientDto
    {
        [StringLength(150)]
        public string? FullName { get; set; }

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(10)]
        public string? BloodGroup { get; set; }

        [Phone]
        [StringLength(20)]
        public string? EmergencyContact { get; set; }

        public int? AddressId { get; set; }
    }
}
