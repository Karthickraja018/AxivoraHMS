using Axivora.Models;

namespace Axivora.Repositories.Interfaces
{
    public interface IAppointmentSlotRepository
    {
        Task<AppointmentSlot?> GetByIdAsync(int id);
        Task<IEnumerable<AppointmentSlot>> GetAvailableSlotsByDoctorAndDateAsync(int doctorId, DateOnly date);
        Task<IEnumerable<AppointmentSlot>> GetSlotsByAvailabilityDayAsync(int availabilityDayId);
        Task<bool> AnyExistForDayAsync(int availabilityDayId);
        Task AddRangeAsync(IEnumerable<AppointmentSlot> slots);
        Task SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task<IEnumerable<AppointmentSlot>> GetSlotsByDoctorAndDateRangeAsync(int doctorId, DateOnly from, DateOnly to);
        Task<IEnumerable<AppointmentSlot>> GetAvailableSlotsByDoctorAndDateRangeAsync(int doctorId, DateOnly from, DateOnly to);
    }
}
