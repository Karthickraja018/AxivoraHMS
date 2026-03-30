using Axivora.DTOs;
using Axivora.Models;
using Axivora.Helpers;

namespace Axivora.Services.Interfaces
{
    public interface IDoctorService
    {
        Task<IEnumerable<DoctorDto>> GetAllDoctorsAsync();
        Task<PaginationResponse<DoctorDto>> GetAllDoctorsAsync(DoctorQueryParams queryParams);
        Task<DoctorDto> GetDoctorByIdAsync(int doctorId);
        Task<DoctorDto?> GetDoctorByUserIdAsync(int userId);
        Task InviteDoctorAsync(InviteDoctorDto dto);
        Task<DoctorDto> CompleteDoctorProfileAsync(int userId, CompleteDoctorProfileDto dto);
        Task<DoctorDto> UpdateMyDoctorProfileAsync(int userId, UpdateMyDoctorProfileDto dto);
        Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto createDoctorDto);
        Task<DoctorDto> UpdateDoctorAsync(int doctorId, UpdateDoctorDto updateDoctorDto);
        Task<bool> DeleteDoctorAsync(int doctorId);
        Task<IEnumerable<DoctorDto>> GetDoctorsByDepartmentAsync(int departmentId);
    }
}
