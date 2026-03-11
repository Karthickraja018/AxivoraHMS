using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

namespace Axivora.Repositories
{
    public class MedicalHistoryRepository : IMedicalHistoryRepository
    {
        private readonly AxivoraDbContext _context;

        public MedicalHistoryRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        private IQueryable<Patient> FullHistoryQuery() =>
            _context.Patients
                .Include(p => p.PatientAllergies)
                .Include(p => p.Appointments.Where(a => !a.IsDeleted))
                    .ThenInclude(a => a.Status)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Consultation)
                        .ThenInclude(c => c!.ICDCode)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Consultation)
                        .ThenInclude(c => c!.Prescriptions)
                            .ThenInclude(pr => pr.Medicine)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Consultation)
                        .ThenInclude(c => c!.OrderedTests)
                            .ThenInclude(ot => ot.LabTest);

        public async Task<Patient?> GetPatientWithFullHistoryByIdAsync(int patientId) =>
            await FullHistoryQuery()
                .FirstOrDefaultAsync(p => p.PatientId == patientId && !p.IsDeleted);

        public async Task<Patient?> GetPatientWithFullHistoryByUserIdAsync(int userId) =>
            await FullHistoryQuery()
                .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);
    }
}
