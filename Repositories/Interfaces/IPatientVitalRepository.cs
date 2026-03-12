using Axivora.Models;

namespace Axivora.Repositories.Interfaces
{
    public interface IPatientVitalRepository
    {
        Task<bool> PatientExistsAsync(int patientId);
        Task<int> CountByPatientAsync(int patientId);
        Task<IEnumerable<PatientVital>> GetPagedByPatientAsync(int patientId, int skip, int take);
        Task<PatientVital?> GetByIdAsync(int vitalId);
        Task AddAsync(PatientVital vital);
        void Remove(PatientVital vital);
        Task SaveChangesAsync();
    }
}
