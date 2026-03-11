using Axivora.Models;

namespace Axivora.Repositories.Interfaces
{
    public interface IMedicalHistoryRepository
    {
        Task<Patient?> GetPatientWithFullHistoryByIdAsync(int patientId);
        Task<Patient?> GetPatientWithFullHistoryByUserIdAsync(int userId);
    }
}
