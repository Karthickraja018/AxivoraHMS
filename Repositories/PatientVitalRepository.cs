using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

namespace Axivora.Repositories
{
    public class PatientVitalRepository : IPatientVitalRepository
    {
        private readonly AxivoraDbContext _context;

        public PatientVitalRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        public async Task<bool> PatientExistsAsync(int patientId) =>
            await _context.Patients.AnyAsync(p => p.PatientId == patientId && !p.IsDeleted);

        public async Task<int> CountByPatientAsync(int patientId) =>
            await _context.PatientVitals.CountAsync(pv => pv.PatientId == patientId);

        public async Task<IEnumerable<PatientVital>> GetPagedByPatientAsync(int patientId, int skip, int take) =>
            await _context.PatientVitals
                .Where(pv => pv.PatientId == patientId)
                .OrderByDescending(pv => pv.RecordedAt)
                .Skip(skip).Take(take)
                .ToListAsync();

        public async Task<PatientVital?> GetByIdAsync(int vitalId) =>
            await _context.PatientVitals.FirstOrDefaultAsync(pv => pv.VitalId == vitalId);

        public async Task AddAsync(PatientVital vital) =>
            await _context.PatientVitals.AddAsync(vital);

        public void Remove(PatientVital vital) =>
            _context.PatientVitals.Remove(vital);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}
