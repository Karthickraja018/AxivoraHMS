using Axivora.DTOs;

namespace Axivora.Services.Interfaces
{
    public interface IAppointmentBookingService
    {
        Task<AppointmentDto> BookAsync(CreateAppointmentDto dto, int callerUserId);
        Task<AppointmentDto> RescheduleAsync(int appointmentId, RescheduleAppointmentDto dto, int callerUserId, string callerRole);
    }
}
