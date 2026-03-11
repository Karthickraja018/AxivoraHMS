using Axivora.DTOs;

namespace Axivora.Services.Interfaces
{
    public interface ISlotService
    {
        /// <summary>Returns all Available slots for a doctor on a given date.</summary>
        Task<IEnumerable<SlotDto>> GetAvailableSlotsAsync(int doctorId, DateOnly date);

        /// <summary>
        /// Generates and persists AppointmentSlot records for a given availability day.
        /// Idempotent — skips generation if slots already exist for the day.
        /// </summary>
        Task GenerateSlotsForDayAsync(int availabilityDayId);
    }
}
