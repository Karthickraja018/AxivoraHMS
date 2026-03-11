using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

namespace Axivora.Repositories
{
    public class DoctorScheduleRepository : IDoctorScheduleRepository
    {
        private readonly AxivoraDbContext _context;

        public DoctorScheduleRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        public async Task<Doctor?> GetDoctorByIdAsync(int doctorId) =>
            await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == doctorId && !d.IsDeleted);

        public async Task<Doctor?> GetDoctorByUserIdAsync(int userId) =>
            await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId && !d.IsDeleted);

        public async Task<IEnumerable<DoctorSchedule>> GetByDoctorIdAsync(int doctorId) =>
            await _context.DoctorSchedules
                .Where(s => s.DoctorId == doctorId)
                .ToListAsync();

        public async Task<DoctorSchedule?> GetByIdWithDoctorAsync(int scheduleId) =>
            await _context.DoctorSchedules
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);

        public async Task<DoctorSchedule?> GetByIdAsync(int scheduleId) =>
            await _context.DoctorSchedules.FindAsync(scheduleId);

        public async Task<IEnumerable<DoctorSchedule>> GetActiveSiblingSchedulesAsync(
            int doctorId, int dayOfWeek, int? excludeScheduleId = null)
        {
            var query = _context.DoctorSchedules
                .Where(s => s.DoctorId == doctorId && s.IsActive && s.DayOfWeek == dayOfWeek);

            if (excludeScheduleId.HasValue)
                query = query.Where(s => s.ScheduleId != excludeScheduleId.Value);

            return await query.ToListAsync();
        }

        public async Task AddScheduleAsync(DoctorSchedule schedule) =>
            await _context.DoctorSchedules.AddAsync(schedule);

        public Task RemoveScheduleAsync(DoctorSchedule schedule)
        {
            _context.DoctorSchedules.Remove(schedule);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}
