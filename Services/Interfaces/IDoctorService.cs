using Axivora.DTOs;
using Axivora.Models;

namespace Axivora.Services.Interfaces
{
    public interface IDoctorService
    {
        Task<IEnumerable<DoctorDto>> GetAllDoctorsAsync();
        Task<PaginationResponse<DoctorDto>> GetAllDoctorsAsync(PaginationParams paginationParams);
        Task<DoctorDto> GetDoctorByIdAsync(int doctorId);
        Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto createDoctorDto);
        Task<DoctorDto> UpdateDoctorAsync(int doctorId, UpdateDoctorDto updateDoctorDto);
        Task<bool> DeleteDoctorAsync(int doctorId);
        Task<IEnumerable<DoctorDto>> GetDoctorsByDepartmentAsync(int departmentId);
    }
}
