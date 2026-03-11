using Axivora.DTOs;

namespace Axivora.Services.Interfaces
{
    public interface ISlotService
    {
        /// <summary>
        /// Returns all Available slots for a doctor on a given date.
        /// Generates slots on demand if the availability day exists but has no slots yet.
        /// </summary>
        Task<IEnumerable<SlotDto>> GetAvailableSlotsAsync(int doctorId, DateOnly date);

        /// <summary>Returns the full detail of a single slot by its ID.</summary>
        Task<SlotDetailDto> GetSlotDetailAsync(int slotId);

        /// <summary>Updates the status of a slot. Admin only.</summary>
        Task<SlotDetailDto> UpdateSlotStatusAsync(int slotId, UpdateSlotStatusDto dto);

        /// <summary>Returns an aggregated daily calendar for a doctor over a date range.</summary>
        Task<IEnumerable<DoctorCalendarDayDto>> GetDoctorCalendarAsync(int doctorId, DateOnly from, DateOnly to);

        /// <summary>Returns per-day available slot counts for a doctor over a date range.</summary>
        Task<IEnumerable<PatientAvailabilityPreviewDto>> GetAvailabilityPreviewAsync(int doctorId, DateOnly from, DateOnly to);

        /// <summary>
        /// Marks availability days in the given range as Leave and blocks all Available slots.
        /// </summary>
        Task ApplyLeaveAsync(int doctorId, DoctorLeaveDto dto);

        /// <summary>
        /// Generates and persists AppointmentSlot records for a given availability day.
        /// Idempotent — skips generation if slots already exist for the day.
        /// </summary>
        Task GenerateSlotsForDayAsync(int availabilityDayId);

        /// <summary>
        /// Ensures slots exist for a doctor on the given date, generating them on demand
        /// from the availability day record if none have been created yet.
        /// Idempotent — safe to call repeatedly.
        /// </summary>
        Task EnsureSlotsGeneratedAsync(int doctorId, DateOnly date);
    }
}
