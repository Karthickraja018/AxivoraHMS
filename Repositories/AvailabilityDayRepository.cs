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

        public async Task<DoctorAvailabilityDay?> GetByIdAsync(int id) =>
            await _context.DoctorAvailabilityDays.FindAsync(id);

        public async Task<DoctorAvailabilityDay?> GetByIdWithSlotsAsync(int id) =>
            await _context.DoctorAvailabilityDays
                .Include(d => d.Slots)
                .FirstOrDefaultAsync(d => d.Id == id);

        public async Task<DoctorAvailabilityDay?> GetByDoctorAndDateAsync(int doctorId, DateOnly date) =>
            await _context.DoctorAvailabilityDays
                .Include(d => d.Slots)
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId && d.Date == date);

        public async Task<IEnumerable<DoctorAvailabilityDay>> GetByDoctorIdAsync(int doctorId) =>
            await _context.DoctorAvailabilityDays
                .Include(d => d.Slots)
                .Where(d => d.DoctorId == doctorId)
                .OrderBy(d => d.Date)
                .ToListAsync();

        public async Task<bool> ExistsAsync(int doctorId, DateOnly date) =>
            await _context.DoctorAvailabilityDays
                .AnyAsync(d => d.DoctorId == doctorId && d.Date == date);

        public async Task AddAsync(DoctorAvailabilityDay day) =>
            await _context.DoctorAvailabilityDays.AddAsync(day);

        public async Task AddRangeAsync(IEnumerable<DoctorAvailabilityDay> days) =>
            await _context.DoctorAvailabilityDays.AddRangeAsync(days);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}
