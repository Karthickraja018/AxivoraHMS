using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

namespace Axivora.Repositories
{
    public class AvailabilityTemplateRepository : IAvailabilityTemplateRepository
    {
        private readonly AxivoraDbContext _context;

        public AvailabilityTemplateRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        public async Task<Doctor?> GetDoctorByIdAsync(int doctorId) =>
            await _context.Doctors
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId && !d.IsDeleted);

        public async Task<DoctorAvailabilityTemplate?> GetByIdAsync(int id) =>
            await _context.DoctorAvailabilityTemplates.FindAsync(id);

        public async Task<DoctorAvailabilityTemplate?> GetByIdWithDoctorAsync(int id) =>
            await _context.DoctorAvailabilityTemplates
                .Include(t => t.Doctor)
                .FirstOrDefaultAsync(t => t.Id == id);

        public async Task<IEnumerable<DoctorAvailabilityTemplate>> GetByDoctorIdAsync(int doctorId) =>
            await _context.DoctorAvailabilityTemplates
                .Include(t => t.Doctor)
                .Where(t => t.DoctorId == doctorId)
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.StartTime)
                .ToListAsync();

        public async Task<IEnumerable<DoctorAvailabilityTemplate>> GetActiveTemplatesAsync(int? doctorId = null)
        {
            var query = _context.DoctorAvailabilityTemplates
                .Where(t => t.IsActive &&
                    (t.EffectiveToDate == null || t.EffectiveToDate >= DateOnly.FromDateTime(DateTime.UtcNow)));

            if (doctorId.HasValue)
                query = query.Where(t => t.DoctorId == doctorId.Value);

            return await query.ToListAsync();
        }

        public async Task AddAsync(DoctorAvailabilityTemplate template) =>
            await _context.DoctorAvailabilityTemplates.AddAsync(template);

        public void Delete(DoctorAvailabilityTemplate template) =>
            _context.DoctorAvailabilityTemplates.Remove(template);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}
