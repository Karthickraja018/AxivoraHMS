using Axivora.Models;

namespace Axivora.Repositories.Interfaces
{
    public interface IAvailabilityDayRepository
    {
        Task<DoctorAvailabilityDay?> GetByIdAsync(int id);
        Task<DoctorAvailabilityDay?> GetByIdWithSlotsAsync(int id);
        Task<DoctorAvailabilityDay?> GetByDoctorAndDateAsync(int doctorId, DateOnly date);
        Task<IEnumerable<DoctorAvailabilityDay>> GetByDoctorIdAsync(int doctorId);

        /// <summary>Returns availability days for a doctor within a date range, including their slots.</summary>
        Task<IEnumerable<DoctorAvailabilityDay>> GetByDoctorAndDateRangeAsync(int doctorId, DateOnly from, DateOnly to);

        /// <summary>Check whether a day record already exists for a given doctor and date.</summary>
        Task<bool> ExistsAsync(int doctorId, DateOnly date);

        Task AddAsync(DoctorAvailabilityDay day);
        Task AddRangeAsync(IEnumerable<DoctorAvailabilityDay> days);
        Task SaveChangesAsync();
    }
}
