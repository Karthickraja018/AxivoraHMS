using Axivora.DTOs;

namespace Axivora.Services.Interfaces
{
    public interface IMedicalHistoryService
    {
        Task<MedicalHistoryDto> GetMedicalHistoryByPatientIdAsync(int patientId);
        Task<MedicalHistoryDto> GetMyMedicalHistoryAsync(int userId);
    }
}
