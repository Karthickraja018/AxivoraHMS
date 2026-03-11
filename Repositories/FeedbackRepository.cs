using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

namespace Axivora.Repositories
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly AxivoraDbContext _context;

        public FeedbackRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        private IQueryable<SessionFeedback> FeedbackWithNavigationsQuery() =>
            _context.SessionFeedbacks
                .Include(f => f.Consultation)
                    .ThenInclude(c => c!.Appointment)
                        .ThenInclude(a => a!.Doctor)
                .Include(f => f.Patient);

        public async Task<Patient?> GetPatientByUserIdAsync(int userId) =>
            await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);

        public async Task<Patient?> GetPatientByIdAsync(int patientId) =>
            await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == patientId && !p.IsDeleted);

        public async Task<Doctor?> GetDoctorByUserIdAsync(int userId) =>
            await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId && !d.IsDeleted);

        public async Task<Consultation?> GetConsultationWithAppointmentAsync(int consultationId) =>
            await _context.Consultations
                .Include(c => c.Appointment)
                    .ThenInclude(a => a!.Status)
                .Include(c => c.Appointment)
                    .ThenInclude(a => a!.Doctor)
                .FirstOrDefaultAsync(c => c.ConsultationId == consultationId);

        public async Task<SessionFeedback?> GetByIdWithNavigationsAsync(int feedbackId) =>
            await FeedbackWithNavigationsQuery()
                .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId);

        public async Task<SessionFeedback?> GetByConsultationIdAsync(int consultationId) =>
            await FeedbackWithNavigationsQuery()
                .FirstOrDefaultAsync(f => f.ConsultationId == consultationId);

        public async Task<bool> FeedbackExistsForConsultationAsync(int consultationId) =>
            await _context.SessionFeedbacks.AnyAsync(f => f.ConsultationId == consultationId);

        public async Task<IEnumerable<SessionFeedback>> GetByDoctorIdAsync(int doctorId) =>
            await FeedbackWithNavigationsQuery()
                .Where(f => f.Consultation!.Appointment!.DoctorId == doctorId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<SessionFeedback>> GetByPatientIdAsync(int patientId) =>
            await FeedbackWithNavigationsQuery()
                .Where(f => f.PatientId == patientId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

        public async Task<SessionFeedback?> GetForDeleteAsync(int feedbackId) =>
            await _context.SessionFeedbacks.FirstOrDefaultAsync(f => f.FeedbackId == feedbackId);

        public async Task AddFeedbackAsync(SessionFeedback feedback) =>
            await _context.SessionFeedbacks.AddAsync(feedback);

        public Task RemoveFeedbackAsync(SessionFeedback feedback)
        {
            _context.SessionFeedbacks.Remove(feedback);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}
