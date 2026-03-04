using Axivora.DTOs;
using Axivora.Models;

namespace Axivora.Services.Interfaces
{
    public interface IPatientService
    {
        Task<IEnumerable<PatientDto>> GetAllPatientsAsync();
        Task<PaginationResponse<PatientDto>> GetAllPatientsAsync(PaginationParams paginationParams);
        Task<PatientDto> GetPatientByIdAsync(int patientId);
        Task<PatientDto> GetPatientByMRNAsync(string mrn);
        Task<PatientDto> GetPatientByUserIdAsync(int userId);
        
        // For authenticated users completing their profile
        Task<PatientDto> CompleteProfileAsync(int userId, CompletePatientProfileDto profileDto);
        
        // Admin only - creates user + patient in one call
        Task<PatientDto> CreatePatientAsync(CreatePatientDto createPatientDto);
        
        Task<PatientDto> UpdatePatientAsync(int patientId, UpdatePatientDto updatePatientDto);
        Task<bool> DeletePatientAsync(int patientId);
        Task<IEnumerable<PatientDto>> SearchPatientsAsync(string searchTerm);
    }
}
