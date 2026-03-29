using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

namespace Axivora.Repositories
{
    public class AvailabilityDayRepository : IAvailabilityDayRepository
    {
        private readonly AxivoraDbContext _context;

        public AvailabilityDayRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Fetches a tracked day record so callers can mutate and save it.
        /// Tracking is intentionally kept — do NOT add AsNoTracking.
        /// </summary>
        public async Task<DoctorAvailabilityDay?> GetByIdAsync(int id) =>
            await _context.DoctorAvailabilityDays.FindAsync(id);

        /// <summary>
        /// Fetches a tracked day including its slots so callers can update slot statuses.
        /// Tracking is intentionally kept — do NOT add AsNoTracking.
        /// </summary>
        public async Task<DoctorAvailabilityDay?> GetByIdWithSlotsAsync(int id) =>
            await _context.DoctorAvailabilityDays
                .Include(d => d.Slots)
                .FirstOrDefaultAsync(d => d.Id == id);

        /// <summary>
        /// Fetches a tracked day including its slots for on-demand slot generation.
        /// Tracking is intentionally kept — do NOT add AsNoTracking.
        /// </summary>
        public async Task<DoctorAvailabilityDay?> GetByDoctorAndDateAsync(int doctorId, DateOnly date) =>
            await _context.DoctorAvailabilityDays
                .Include(d => d.Slots)
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId && d.Date == date);

        public async Task<IEnumerable<DoctorAvailabilityDay>> GetByDoctorIdAsync(int doctorId) =>
            await _context.DoctorAvailabilityDays
                .AsNoTracking()
                .Include(d => d.Slots)
                .Where(d => d.DoctorId == doctorId)
                .OrderBy(d => d.Date)
                .ToListAsync();

        public async Task<IEnumerable<DoctorAvailabilityDay>> GetByDoctorAndDateRangeAsync(
            int doctorId, DateOnly from, DateOnly to) =>
            await _context.DoctorAvailabilityDays
                .AsNoTracking()
                .Include(d => d.Slots)
                .Where(d => d.DoctorId == doctorId && d.Date >= from && d.Date <= to)
                .OrderBy(d => d.Date)
                .ToListAsync();

        public async Task<IEnumerable<DoctorAvailabilityDay>> GetByDoctorAndDateRangeNoSlotsAsync(int doctorId, DateOnly from, DateOnly to) =>
            await _context.DoctorAvailabilityDays
                .AsNoTracking()
                .Where(d => d.DoctorId == doctorId && d.Date >= from && d.Date <= to)
                .ToListAsync();

        public async Task<HashSet<DateOnly>> GetDatesByDoctorAndRangeAsync(int doctorId, DateOnly from, DateOnly to)
        {
            var dates = await _context.DoctorAvailabilityDays
                .AsNoTracking()
                .Where(d => d.DoctorId == doctorId && d.Date >= from && d.Date <= to)
                .Select(d => d.Date)
                .ToListAsync();
            return dates.ToHashSet();
        }

        public async Task<int> RemoveOpenDaysAsync(int doctorId, DateOnly from, DateOnly to, int? sourceTemplateId = null)
        {
            var daysQuery = _context.DoctorAvailabilityDays.AsQueryable();

            daysQuery = daysQuery.Where(d => d.DoctorId == doctorId && d.Date >= from && d.Date <= to && d.Status == AvailabilityDayStatus.Open);

            if (sourceTemplateId.HasValue)
                daysQuery = daysQuery.Where(d => d.SourceTemplateId == sourceTemplateId.Value);

            // Filter days that have booked slots
            var days = await daysQuery
                .Where(d => !d.Slots.Any(s => s.Status == SlotStatus.Booked))
                .ToListAsync();

            if (!days.Any()) return 0;

            _context.DoctorAvailabilityDays.RemoveRange(days);
            await _context.SaveChangesAsync();
            return days.Count;
        }

        public async Task<bool> ExistsAsync(int doctorId, DateOnly date) =>
            await _context.DoctorAvailabilityDays
                .AsNoTracking()
                .AnyAsync(d => d.DoctorId == doctorId && d.Date == date);

        public async Task AddAsync(DoctorAvailabilityDay day) =>
            await _context.DoctorAvailabilityDays.AddAsync(day);

        public async Task AddRangeAsync(IEnumerable<DoctorAvailabilityDay> days) =>
            await _context.DoctorAvailabilityDays.AddRangeAsync(days);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}
