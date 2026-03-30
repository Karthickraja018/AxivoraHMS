using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;
using Axivora.Repositories.Interfaces;

namespace Axivora.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IFeedbackRepository _repository;

        public FeedbackService(IFeedbackRepository repository)
        {
            _repository = repository;
        }

        private static readonly IReadOnlyDictionary<int, string> _ratingLabels =
            new Dictionary<int, string>
            {
                [1] = "Very Poor",
                [2] = "Poor",
                [3] = "Average",
                [4] = "Good",
                [5] = "Excellent"
            };

        private static string RatingLabel(int rating) =>
            _ratingLabels.TryGetValue(rating, out var label) ? label : rating.ToString();

        private static SessionFeedbackDto MapToDto(SessionFeedback f) => new()
        {
            FeedbackId     = f.FeedbackId,
            ConsultationId = f.ConsultationId,
            PatientId      = f.PatientId,
            PatientName    = f.Patient?.FullName ?? string.Empty,
            DoctorId       = f.Consultation?.Appointment?.DoctorId ?? 0,
            DoctorName     = f.Consultation?.Appointment?.Doctor?.FullName ?? string.Empty,
            Rating         = f.Rating,
            RatingLabel    = RatingLabel(f.Rating),
            Comment        = f.Comment,
            CreatedAt      = f.CreatedAt,
            IsEdited       = f.IsEdited,
            UpdatedAt      = f.UpdatedAt
        };

        public async Task<SessionFeedbackDto> CreateFeedbackAsync(CreateFeedbackDto dto, int callerUserId)
        {
            var patient = await _repository.GetPatientByUserIdAsync(callerUserId)
                ?? throw new KeyNotFoundException("Patient profile not found for the authenticated user.");

            var consultation = await _repository.GetConsultationWithAppointmentAsync(dto.ConsultationId)
                ?? throw new KeyNotFoundException($"Consultation with ID {dto.ConsultationId} not found.");

            if (consultation.Appointment!.PatientId != patient.PatientId)
                throw new UnauthorizedAccessException("You can only submit feedback for your own consultations.");

            var statusName = consultation.Appointment.Status?.StatusName ?? string.Empty;
            if (statusName != "Completed")
                throw new InvalidOperationException("Feedback can only be submitted for completed consultations.");

            if (await _repository.FeedbackExistsForConsultationAsync(dto.ConsultationId))
                throw new InvalidOperationException("Feedback has already been submitted for this consultation.");

            var feedback = new SessionFeedback
            {
                ConsultationId = dto.ConsultationId,
                PatientId      = patient.PatientId,
                Rating         = dto.Rating,
                Comment        = dto.Comment,
                CreatedAt      = DateTime.UtcNow,
                IsEdited       = false
            };

            await _repository.AddFeedbackAsync(feedback);
            await _repository.SaveChangesAsync();

            feedback.Patient      = patient;
            feedback.Consultation = consultation;

            return MapToDto(feedback);
        }

        public async Task<SessionFeedbackDto> UpdateFeedbackAsync(int feedbackId, UpdateFeedbackDto dto, int callerUserId)
        {
            var feedback = await _repository.GetByIdWithNavigationsAsync(feedbackId)
                ?? throw new KeyNotFoundException($"Feedback with ID {feedbackId} not found.");

            var patient = await _repository.GetPatientByUserIdAsync(callerUserId)
                ?? throw new KeyNotFoundException("Patient profile not found for the authenticated user.");

            if (feedback.PatientId != patient.PatientId)
                throw new UnauthorizedAccessException("You can only edit your own feedback.");

            if (dto.Rating.HasValue)
                feedback.Rating = dto.Rating.Value;

            if (dto.Comment != null)
                feedback.Comment = dto.Comment;

            feedback.IsEdited  = true;
            feedback.UpdatedAt = DateTime.UtcNow;

            await _repository.SaveChangesAsync();

            return MapToDto(feedback);
        }

        public async Task<SessionFeedbackDto> GetFeedbackByConsultationAsync(int consultationId, int callerUserId, string callerRole)
        {
            var feedback = await _repository.GetByConsultationIdAsync(consultationId)
                ?? throw new KeyNotFoundException($"No feedback found for consultation ID {consultationId}.");

            if (callerRole == "Patient")
            {
                var patient = await _repository.GetPatientByUserIdAsync(callerUserId);
                if (patient == null || feedback.PatientId != patient.PatientId)
                    throw new UnauthorizedAccessException("You can only view your own feedback.");
            }
            else if (callerRole == "Doctor")
            {
                var doctor = await _repository.GetDoctorByUserIdAsync(callerUserId);
                var appointmentDoctorId = feedback.Consultation?.Appointment?.DoctorId;
                if (doctor == null || appointmentDoctorId != doctor.DoctorId)
                    throw new UnauthorizedAccessException("You can only view feedback for your own consultations.");
            }

            return MapToDto(feedback);
        }

        public async Task<IEnumerable<SessionFeedbackDto>> GetFeedbackByDoctorAsync(int doctorId, int callerUserId, string callerRole)
        {
            if (callerRole == "Doctor")
            {
                var callerDoctor = await _repository.GetDoctorByUserIdAsync(callerUserId);
                if (callerDoctor == null || callerDoctor.DoctorId != doctorId)
                    throw new UnauthorizedAccessException("You can only view feedback for your own consultations.");
            }

            // Patient + Admin: read-only directory view; no extra checks.

            var feedbacks = await _repository.GetByDoctorIdAsync(doctorId);
            return feedbacks.Select(MapToDto);
        }

        public async Task<IEnumerable<SessionFeedbackDto>> GetFeedbackByPatientAsync(int patientId, int callerUserId, string callerRole)
        {
            if (callerRole == "Patient")
            {
                var patient = await _repository.GetPatientByUserIdAsync(callerUserId);
                if (patient == null || patient.PatientId != patientId)
                    throw new UnauthorizedAccessException("You can only view your own feedback.");
            }

            var feedbacks = await _repository.GetByPatientIdAsync(patientId);
            return feedbacks.Select(MapToDto);
        }

        public async Task DeleteFeedbackAsync(int feedbackId, int callerUserId, string callerRole)
        {
            var feedback = await _repository.GetForDeleteAsync(feedbackId)
                ?? throw new KeyNotFoundException($"Feedback with ID {feedbackId} not found.");

            if (callerRole != "Admin")
            {
                var patient = await _repository.GetPatientByUserIdAsync(callerUserId);
                if (patient == null || feedback.PatientId != patient.PatientId)
                    throw new UnauthorizedAccessException("You can only delete your own feedback.");
            }

            await _repository.RemoveFeedbackAsync(feedback);
            await _repository.SaveChangesAsync();
        }
    }
}
