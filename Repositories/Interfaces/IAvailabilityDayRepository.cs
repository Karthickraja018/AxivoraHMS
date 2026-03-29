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

        /// <summary>Returns availability days without slots for lightweight checks.</summary>
        Task<IEnumerable<DoctorAvailabilityDay>> GetByDoctorAndDateRangeNoSlotsAsync(int doctorId, DateOnly from, DateOnly to);

        /// <summary>Returns only the dates for which availability days already exist in a given range.</summary>
        Task<HashSet<DateOnly>> GetDatesByDoctorAndRangeAsync(int doctorId, DateOnly from, DateOnly to);

        /// <summary>Deletes availability days that are Open and have zero booked slots.</summary>
        Task<int> RemoveOpenDaysAsync(int doctorId, DateOnly from, DateOnly to, int? sourceTemplateId = null);

        /// <summary>Check whether a day record already exists for a given doctor and date.</summary>
        Task<bool> ExistsAsync(int doctorId, DateOnly date);

        Task AddAsync(DoctorAvailabilityDay day);
        Task AddRangeAsync(IEnumerable<DoctorAvailabilityDay> days);
        Task SaveChangesAsync();
    }
}
