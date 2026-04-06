using Axivora.DTOs;

namespace Axivora.Services.Interfaces
{
    public interface IAppointmentLifecycleService
    {
        Task<AppointmentDto> UpdateAppointmentStatusAsync(int appointmentId, string statusName);
        Task<AppointmentDto> UpdateAppointmentStatusAsync(int appointmentId, string statusName, int callerUserId, string callerRole);
        Task<AppointmentDto> CancelAsync(int appointmentId, int callerUserId, string callerRole);
        Task<AppointmentDto> StartAsync(int appointmentId, int callerUserId, string callerRole);
        Task<AppointmentDto> EndAsync(int appointmentId, int callerUserId, string callerRole);
        Task<AppointmentDto> CompleteAsync(int appointmentId, int callerUserId, string callerRole);
        Task<int> AutoMarkNoShowsAsync(DateTime utcNow, CancellationToken ct);
    }
}
