using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Axivora.Data;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

namespace Axivora.Repositories
{
    public class AppointmentSlotRepository : IAppointmentSlotRepository
    {
        private readonly AxivoraDbContext _context;
        private IDbContextTransaction? _transaction;

        public AppointmentSlotRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        public async Task<AppointmentSlot?> GetByIdAsync(int id) =>
            await _context.AppointmentSlots
                .Include(s => s.AvailabilityDay)
                .FirstOrDefaultAsync(s => s.Id == id);

        public async Task<IEnumerable<AppointmentSlot>> GetAvailableSlotsByDoctorAndDateAsync(
            int doctorId, DateOnly date) =>
            await _context.AppointmentSlots
                .Where(s => s.DoctorId == doctorId &&
                            s.AvailabilityDay!.Date == date &&
                            s.Status == SlotStatus.Available)
                .OrderBy(s => s.SlotStart)
                .ToListAsync();

        public async Task<IEnumerable<AppointmentSlot>> GetSlotsByAvailabilityDayAsync(
            int availabilityDayId) =>
            await _context.AppointmentSlots
                .Where(s => s.AvailabilityDayId == availabilityDayId)
                .OrderBy(s => s.SlotStart)
                .ToListAsync();

        public async Task<bool> AnyExistForDayAsync(int availabilityDayId) =>
            await _context.AppointmentSlots
                .AnyAsync(s => s.AvailabilityDayId == availabilityDayId);

        public async Task AddRangeAsync(IEnumerable<AppointmentSlot> slots) =>
            await _context.AppointmentSlots.AddRangeAsync(slots);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();

        public async Task BeginTransactionAsync() =>
            _transaction = await _context.Database.BeginTransactionAsync();

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
                await _transaction.CommitAsync();
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
                await _transaction.RollbackAsync();
        }
    }
}
