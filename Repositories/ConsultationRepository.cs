using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

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
