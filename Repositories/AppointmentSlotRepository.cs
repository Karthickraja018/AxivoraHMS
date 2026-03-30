using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.SqlClient;
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

        /// <summary>
        /// Fetches a tracked slot so callers can update its status and save.
        /// Tracking is intentionally kept — do NOT add AsNoTracking.
        /// </summary>
        public async Task<AppointmentSlot?> GetByIdAsync(int id) =>
            await _context.AppointmentSlots
                .Include(s => s.AvailabilityDay)
                .FirstOrDefaultAsync(s => s.Id == id);

        public async Task<IEnumerable<AppointmentSlot>> GetAvailableSlotsByDoctorAndDateAsync(
            int doctorId, DateOnly date) =>
            await _context.AppointmentSlots
                .AsNoTracking()
                .Where(s => s.DoctorId == doctorId &&
                            s.AvailabilityDay!.Date == date &&
                            s.Status == SlotStatus.Available)
                .OrderBy(s => s.SlotStart)
                .ToListAsync();

        public async Task<IEnumerable<AppointmentSlot>> GetSlotsByAvailabilityDayAsync(
            int availabilityDayId) =>
            await _context.AppointmentSlots
                .AsNoTracking()
                .Where(s => s.AvailabilityDayId == availabilityDayId)
                .OrderBy(s => s.SlotStart)
                .ToListAsync();

        public async Task<bool> AnyExistForDayAsync(int availabilityDayId) =>
            await _context.AppointmentSlots
                .AsNoTracking()
                .AnyAsync(s => s.AvailabilityDayId == availabilityDayId);

        public async Task<IEnumerable<AppointmentSlot>> GetSlotsByDoctorAndDateRangeAsync(
            int doctorId, DateOnly from, DateOnly to) =>
            await _context.AppointmentSlots
                .AsNoTracking()
                .Include(s => s.AvailabilityDay)
                .Where(s => s.DoctorId == doctorId &&
                            s.AvailabilityDay!.Date >= from &&
                            s.AvailabilityDay!.Date <= to)
                .OrderBy(s => s.SlotStart)
                .ToListAsync();

        public async Task<IEnumerable<AppointmentSlot>> GetAvailableSlotsByDoctorAndDateRangeAsync(
            int doctorId, DateOnly from, DateOnly to) =>
            await _context.AppointmentSlots
                .AsNoTracking()
                .Include(s => s.AvailabilityDay)
                .Where(s => s.DoctorId == doctorId &&
                            s.AvailabilityDay!.Date >= from &&
                            s.AvailabilityDay!.Date <= to &&
                            s.Status == SlotStatus.Available)
                .OrderBy(s => s.SlotStart)
                .ToListAsync();

        public async Task AddRangeAsync(IEnumerable<AppointmentSlot> slots) =>
            await _context.AppointmentSlots.AddRangeAsync(slots);

        public async Task SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsAppointmentSlotsPrimaryKeyViolation(ex))
            {
                // Self-heal: identity seed got out of sync (common in dev DBs after reseeds/restores).
                await ReseedAppointmentSlotsIdentityAsync();
                await _context.SaveChangesAsync();
            }
        }

        private static bool IsAppointmentSlotsPrimaryKeyViolation(DbUpdateException ex)
        {
            if (ex.InnerException is not SqlException sqlEx)
                return false;

            // 2627 = Violation of PRIMARY KEY constraint / unique index
            return sqlEx.Number == 2627 &&
                   sqlEx.Message.Contains("PK_AppointmentSlots", StringComparison.OrdinalIgnoreCase);
        }

        private async Task ReseedAppointmentSlotsIdentityAsync()
        {
            // Reseed to current max so next insert uses MAX+1.
            const string sql = """
                               DECLARE @maxId int = (SELECT ISNULL(MAX([Id]), 0) FROM [AppointmentSlots]);
                               DBCC CHECKIDENT ('[AppointmentSlots]', RESEED, @maxId);
                               """;

            await _context.Database.ExecuteSqlRawAsync(sql);
        }

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
