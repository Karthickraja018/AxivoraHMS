using Axivora.DTOs;

namespace Axivora.Services.Interfaces
{
    public interface IAppointmentBookingService
    {
        /// <summary>
        /// Books a slot for the authenticated patient.
        /// Creates an Appointment record and atomically marks the slot as Booked.
        /// </summary>
        Task<AppointmentDto> BookAsync(BookAppointmentDto dto, int callerUserId);

        /// <summary>
        /// Reschedules an existing appointment to a new slot.
        /// Frees the old slot and atomically marks the new slot as Booked.
        /// </summary>
        Task<AppointmentDto> RescheduleAsync(int appointmentId, SlotRescheduleAppointmentDto dto, int callerUserId, string callerRole);

        /// <summary>
        /// Cancels an appointment and releases its slot back to Available.
        /// </summary>
        Task CancelAsync(int appointmentId, int callerUserId, string callerRole);
    }
}
