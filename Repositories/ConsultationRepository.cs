using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.Models;
using Axivora.Repositories.Interfaces;
using Axivora.Helpers;

namespace Axivora.Repositories
{
    public class ConsultationRepository : IConsultationRepository
    {
        private readonly AxivoraDbContext _context;

        public ConsultationRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        private IQueryable<Consultation> BaseQuery() =>
            _context.Consultations
                .Include(c => c.ICDCode)
                .Include(c => c.Appointment)
                    .ThenInclude(a => a!.Patient)
                .Include(c => c.Appointment)
                    .ThenInclude(a => a!.Doctor)
                .Include(c => c.Prescriptions)
                    .ThenInclude(p => p.Medicine)
                .Include(c => c.OrderedTests)
                    .ThenInclude(ot => ot.LabTest);

        public async Task<IEnumerable<Consultation>> GetAllAsync() =>
            await BaseQuery().ToListAsync();

        public async Task<int> CountAsync() =>
            await _context.Consultations.CountAsync();

        public async Task<IEnumerable<Consultation>> GetPagedAsync(int skip, int take) =>
            await BaseQuery()
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip).Take(take)
                .ToListAsync();

        public async Task<Consultation?> GetByIdAsync(int consultationId) =>
            await BaseQuery()
                .FirstOrDefaultAsync(c => c.ConsultationId == consultationId);

        public async Task<Consultation?> GetByAppointmentIdAsync(int appointmentId) =>
            await BaseQuery()
                .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId);

        public async Task<bool> ExistsForAppointmentAsync(int appointmentId) =>
            await _context.Consultations.AnyAsync(c => c.AppointmentId == appointmentId);

        public async Task<int> CountByPatientAsync(int patientId) =>
            await _context.Consultations
                .Where(c => c.Appointment != null && c.Appointment.PatientId == patientId)
                .CountAsync();

        public async Task<IEnumerable<Consultation>> GetPagedByPatientAsync(int patientId, int skip, int take) =>
            await BaseQuery()
                .Where(c => c.Appointment != null && c.Appointment.PatientId == patientId)
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip).Take(take)
                .ToListAsync();

        public async Task<Doctor?> GetDoctorByUserIdAsync(int userId) =>
            await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId && !d.IsDeleted);

        public async Task<Appointment?> GetAppointmentWithStatusAsync(int appointmentId) =>
            await _context.Appointments
                .Include(a => a.Status)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

        public async Task<Appointment?> GetAppointmentWithPatientAndDoctorAsync(int appointmentId) =>
            await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                    .ThenInclude(p => p!.User)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

        public async Task<int> CountByDoctorAsync(int doctorId) =>
            await _context.Consultations
                .Where(c => c.Appointment != null && c.Appointment.DoctorId == doctorId)
                .CountAsync();

        public async Task<IEnumerable<Consultation>> GetPagedByDoctorAsync(int doctorId, int skip, int take) =>
            await BaseQuery()
                .Where(c => c.Appointment != null && c.Appointment.DoctorId == doctorId)
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip).Take(take)
                .ToListAsync();

        private IQueryable<Consultation> BuildDoctorFilteredQuery(int doctorId, ConsultationDoctorFilterParams filter)
        {
            var query = BaseQuery()
                .Where(c => c.Appointment != null && c.Appointment.DoctorId == doctorId);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                query = query.Where(c =>
                    (c.Appointment != null && c.Appointment.Patient != null && EF.Functions.Like(c.Appointment.Patient.FullName, $"%{s}%")) ||
                    EF.Functions.Like(c.ChiefComplaint ?? string.Empty, $"%{s}%") ||
                    EF.Functions.Like(c.DiagnosisNotes ?? string.Empty, $"%{s}%") ||
                    EF.Functions.Like(c.TreatmentPlan ?? string.Empty, $"%{s}%") ||
                    (c.ICDCode != null && EF.Functions.Like(c.ICDCode.Code, $"%{s}%"))
                );
            }

            if (filter.From.HasValue)
            {
                query = query.Where(c => c.CreatedAt >= filter.From.Value);
            }

            if (filter.To.HasValue)
            {
                var toExclusive = filter.To.Value.Date.AddDays(1);
                query = query.Where(c => c.CreatedAt < toExclusive);
            }

            if (!string.IsNullOrWhiteSpace(filter.Focus))
            {
                var focus = filter.Focus.Trim().ToLowerInvariant();
                query = focus switch
                {
                    "needsdocumentation" => query.Where(c =>
                        string.IsNullOrWhiteSpace(c.ChiefComplaint) ||
                        string.IsNullOrWhiteSpace(c.DiagnosisNotes) ||
                        string.IsNullOrWhiteSpace(c.TreatmentPlan)),
                    "haslabs" => query.Where(c => c.OrderedTests.Any()),
                    "hasprescriptions" => query.Where(c => c.Prescriptions.Any()),
                    "hasicd" => query.Where(c => c.ICDId != null),
                    _ => query,
                };
            }

            return query;
        }

        public async Task<int> CountByDoctorFilteredAsync(int doctorId, ConsultationDoctorFilterParams filter) =>
            await BuildDoctorFilteredQuery(doctorId, filter).CountAsync();

        public async Task<IEnumerable<Consultation>> GetPagedByDoctorFilteredAsync(int doctorId, ConsultationDoctorFilterParams filter, int skip, int take) =>
            await BuildDoctorFilteredQuery(doctorId, filter)
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip).Take(take)
                .ToListAsync();

        public async Task AddConsultationAsync(Consultation consultation) =>
            await _context.Consultations.AddAsync(consultation);

        public async Task<bool> IsMedicineAlreadyPrescribedAsync(int consultationId, int medicineId) =>
            await _context.Prescriptions
                .AnyAsync(p => p.ConsultationId == consultationId && p.MedicineId == medicineId);

        public async Task AddPrescriptionAsync(Prescription prescription) =>
            await _context.Prescriptions.AddAsync(prescription);

        public async Task AddOrderedTestAsync(OrderedTest orderedTest) =>
            await _context.OrderedTests.AddAsync(orderedTest);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}
