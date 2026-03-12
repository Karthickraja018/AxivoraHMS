using Axivora.DTOs;
using Axivora.Helpers;

namespace Axivora.Services.Interfaces
{
    public interface IPatientVitalService
    {
        Task<PaginationResponse<PatientVitalDto>> GetVitalsAsync(int patientId, PaginationParams paginationParams);
        Task<PatientVitalDto> GetVitalByIdAsync(int patientId, int vitalId);
        Task<PatientVitalDto> CreateVitalAsync(int patientId, CreatePatientVitalDto dto);
        Task<PatientVitalDto> UpdateVitalAsync(int patientId, int vitalId, UpdatePatientVitalDto dto);
        Task DeleteVitalAsync(int patientId, int vitalId);
    }
}
