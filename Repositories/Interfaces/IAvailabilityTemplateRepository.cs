using Axivora.Models;

namespace Axivora.Repositories.Interfaces
{
    public interface IAvailabilityTemplateRepository
    {
        Task<Doctor?> GetDoctorByIdAsync(int doctorId);
        Task<DoctorAvailabilityTemplate?> GetByIdAsync(int id);
        Task<DoctorAvailabilityTemplate?> GetByIdWithDoctorAsync(int id);
        Task<IEnumerable<DoctorAvailabilityTemplate>> GetByDoctorIdAsync(int doctorId);

        /// <summary>Returns active templates for all doctors that need days generated.</summary>
        Task<IEnumerable<DoctorAvailabilityTemplate>> GetActiveTemplatesAsync(int? doctorId = null);

        Task AddAsync(DoctorAvailabilityTemplate template);
        Task SaveChangesAsync();
    }
}
