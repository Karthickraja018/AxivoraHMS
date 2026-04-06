using Axivora.DTOs;
using Axivora.Helpers;

namespace Axivora.Services.Interfaces
{
    public interface IAppointmentService : IAppointmentReadService, IAppointmentBookingService, IAppointmentLifecycleService
    {
        Task<AppointmentDto> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto updateAppointmentDto, int callerUserId, string callerRole);
        Task<AppointmentDto> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto updateAppointmentDto);
        Task<bool> CancelAppointmentAsync(int appointmentId);
        Task<bool> CancelAppointmentAsync(int appointmentId, int callerUserId, string callerRole);
    }
}
