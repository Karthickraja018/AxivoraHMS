using Axivora.Models;

namespace Axivora.Repositories.Interfaces
{
    public interface IAppointmentBookingRepository
    {
        Task<AppointmentSlot?> GetSlotByIdAsync(int slotId);
        Task<Appointment?> GetAppointmentByIdAsync(int appointmentId);
        Task<Patient?> GetPatientByUserIdAsync(int userId);
        Task<AppointmentStatus?> GetDefaultStatusAsync();
        Task<AppointmentStatus?> GetStatusByNameAsync(string name);
        Task AddAppointmentAsync(Appointment appointment);
        Task SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
