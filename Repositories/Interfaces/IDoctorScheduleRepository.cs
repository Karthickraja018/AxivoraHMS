using Axivora.Models;

namespace Axivora.Repositories.Interfaces
{
    public interface IDoctorScheduleRepository
    {
        Task<Doctor?> GetDoctorByIdAsync(int doctorId);
        Task<Doctor?> GetDoctorByUserIdAsync(int userId);
        Task<IEnumerable<DoctorSchedule>> GetByDoctorIdAsync(int doctorId);
        Task<DoctorSchedule?> GetByIdWithDoctorAsync(int scheduleId);
        Task<DoctorSchedule?> GetByIdAsync(int scheduleId);
        Task<IEnumerable<DoctorSchedule>> GetActiveSiblingSchedulesAsync(int doctorId, int dayOfWeek, int? excludeScheduleId = null);
        Task AddScheduleAsync(DoctorSchedule schedule);
        Task RemoveScheduleAsync(DoctorSchedule schedule);
        Task SaveChangesAsync();
    }
}
