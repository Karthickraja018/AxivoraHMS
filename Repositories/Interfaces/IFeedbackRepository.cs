using Axivora.Models;

namespace Axivora.Repositories.Interfaces
{
    public interface IFeedbackRepository
    {
        Task<Patient?> GetPatientByUserIdAsync(int userId);
        Task<Patient?> GetPatientByIdAsync(int patientId);
        Task<Doctor?> GetDoctorByUserIdAsync(int userId);
        Task<Consultation?> GetConsultationWithAppointmentAsync(int consultationId);
        Task<SessionFeedback?> GetByIdWithNavigationsAsync(int feedbackId);
        Task<SessionFeedback?> GetByConsultationIdAsync(int consultationId);
        Task<bool> FeedbackExistsForConsultationAsync(int consultationId);
        Task<IEnumerable<SessionFeedback>> GetByDoctorIdAsync(int doctorId);
        Task<IEnumerable<SessionFeedback>> GetByPatientIdAsync(int patientId);
        Task<SessionFeedback?> GetForDeleteAsync(int feedbackId);
        Task AddFeedbackAsync(SessionFeedback feedback);
        Task RemoveFeedbackAsync(SessionFeedback feedback);
        Task SaveChangesAsync();
    }
}
