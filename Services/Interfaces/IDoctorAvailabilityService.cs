using Axivora.DTOs;

namespace Axivora.Services.Interfaces
{
    public interface IDoctorAvailabilityService
    {
        /// <summary>
        /// Returns all availability days for a doctor, optionally filtered by a date range.
        /// </summary>
        Task<IEnumerable<AvailabilityDayDto>> GetAvailabilityDaysAsync(int doctorId);

        /// <summary>
        /// Updates the status of an availability day.
        /// When set to Leave or Holiday all slots for that day are blocked.
        /// When set back to Open all Blocked slots are made Available again.
        /// </summary>
        Task<AvailabilityDayDto> UpdateDayStatusAsync(int dayId, UpdateAvailabilityDayStatusDto dto);

        /// <summary>
        /// Generates DoctorAvailabilityDay records for the next <paramref name="daysAhead"/> days
        /// based on all active templates. Called by the background service.
        /// </summary>
        Task GenerateAvailabilityDaysAsync(int daysAhead = 30);
    }
}
