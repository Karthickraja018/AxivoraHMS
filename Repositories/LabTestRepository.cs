using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

namespace Axivora.Repositories
{
    public class LabTestRepository : ILabTestRepository
    {
        private readonly AxivoraDbContext _context;

        public LabTestRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        private IQueryable<OrderedTest> OrderedTestWithNavigationsQuery() =>
            _context.OrderedTests
                .Include(ot => ot.LabTest)
                .Include(ot => ot.Consultation)
                    .ThenInclude(c => c!.Appointment)
                        .ThenInclude(a => a!.Patient);

        public async Task<OrderedTest?> GetOrderedTestByIdAsync(int orderedTestId) =>
            await OrderedTestWithNavigationsQuery()
                .FirstOrDefaultAsync(ot => ot.OrderedTestId == orderedTestId);

        public async Task<bool> PatientExistsAsync(int patientId) =>
            await _context.Patients.AnyAsync(p => p.PatientId == patientId && !p.IsDeleted);

        public async Task<bool> ConsultationExistsAsync(int consultationId) =>
            await _context.Consultations.AnyAsync(c => c.ConsultationId == consultationId);

        public async Task<IEnumerable<OrderedTest>> GetByPatientIdAsync(int patientId) =>
            await OrderedTestWithNavigationsQuery()
                .Where(ot => ot.Consultation!.Appointment!.PatientId == patientId)
                .OrderByDescending(ot => ot.ResultDate ?? DateTime.MinValue)
                .ToListAsync();

        public async Task<IEnumerable<OrderedTest>> GetByConsultationIdAsync(int consultationId) =>
            await OrderedTestWithNavigationsQuery()
                .Where(ot => ot.ConsultationId == consultationId)
                .OrderBy(ot => ot.OrderedTestId)
                .ToListAsync();

        public async Task<IEnumerable<OrderedTest>> GetByUserIdAsync(int userId) =>
            await OrderedTestWithNavigationsQuery()
                .Include(ot => ot.Consultation!)
                    .ThenInclude(c => c!.Appointment!)
                        .ThenInclude(a => a!.Doctor)
                .Where(ot => ot.Consultation!.Appointment!.Patient!.UserId == userId)
                .OrderByDescending(ot => ot.ResultDate ?? DateTime.MinValue)
                .ToListAsync();

        public async Task<int> CountCatalogueAsync(string? search)
        {
            var query = _context.LabTests.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(lt => lt.TestName.Contains(search));
            return await query.CountAsync();
        }

        public async Task<IEnumerable<LabTest>> GetCataloguePagedAsync(string? search, int skip, int take)
        {
            var query = _context.LabTests.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(lt => lt.TestName.Contains(search));
            return await query
                .OrderBy(lt => lt.TestName)
                .Skip(skip).Take(take)
                .ToListAsync();
        }

        public async Task<LabTest?> GetCatalogueItemAsync(int id) =>
            await _context.LabTests.FirstOrDefaultAsync(lt => lt.LabTestId == id);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}
