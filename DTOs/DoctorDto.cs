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

    public class CreateDoctorDto
    {
        public string LicenseNumber { get; set; }
        public string FullName { get; set; }
        public string Qualification { get; set; }
        public int? ExperienceYears { get; set; }
        public int? AddressId { get; set; }
        public int UserId { get; set; }
        public List<int> DepartmentIds { get; set; }
    }

    public class UpdateDoctorDto
    {
        public string FullName { get; set; }
        public string Qualification { get; set; }
        public int? ExperienceYears { get; set; }
        public int? AddressId { get; set; }
        public bool IsActive { get; set; }
    }
}
