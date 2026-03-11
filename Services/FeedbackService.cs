using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly AxivoraDbContext _context;

        public FeedbackService(AxivoraDbContext context)
        {
            _context = context;
        }

        // ?? helpers ??????????????????????????????????????????????????????????????

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

        private async Task<SessionFeedback> LoadFeedbackAsync(int feedbackId) =>
            await _context.SessionFeedbacks
                .Include(f => f.Consultation)
                    .ThenInclude(c => c!.Appointment)
                        .ThenInclude(a => a!.Doctor)
                .Include(f => f.Patient)
                .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId)
            ?? throw new KeyNotFoundException($"Feedback with ID {feedbackId} not found.");

        private static SessionFeedbackDto MapToDto(SessionFeedback f) => new()
        {
            FeedbackId    = f.FeedbackId,
            ConsultationId = f.ConsultationId,
            PatientId     = f.PatientId,
            PatientName   = f.Patient?.FullName ?? string.Empty,
            DoctorId      = f.Consultation?.Appointment?.DoctorId ?? 0,
            DoctorName    = f.Consultation?.Appointment?.Doctor?.FullName ?? string.Empty,
            Rating        = f.Rating,
            RatingLabel   = RatingLabel(f.Rating),
            Comment       = f.Comment,
            CreatedAt     = f.CreatedAt,
            IsEdited      = f.IsEdited,
            UpdatedAt     = f.UpdatedAt
        };

        // ?? CreateFeedbackAsync ???????????????????????????????????????????????????

        public async Task<SessionFeedbackDto> CreateFeedbackAsync(CreateFeedbackDto dto, int callerUserId)
        {
            // Resolve the caller's patient profile
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == callerUserId && !p.IsDeleted)
                ?? throw new KeyNotFoundException("Patient profile not found for the authenticated user.");

            // Load the consultation with its appointment and status
            var consultation = await _context.Consultations
                .Include(c => c.Appointment)
                    .ThenInclude(a => a!.Status)
                .Include(c => c.Appointment)
                    .ThenInclude(a => a!.Doctor)
                .FirstOrDefaultAsync(c => c.ConsultationId == dto.ConsultationId)
                ?? throw new KeyNotFoundException($"Consultation with ID {dto.ConsultationId} not found.");

            // Only the patient of the appointment may submit feedback
            if (consultation.Appointment!.PatientId != patient.PatientId)
                throw new UnauthorizedAccessException(
                    "You can only submit feedback for your own consultations.");

            // Feedback is only meaningful once the appointment is completed
            var statusName = consultation.Appointment.Status?.StatusName ?? string.Empty;
            if (statusName != "Completed")
                throw new InvalidOperationException(
                    "Feedback can only be submitted for completed consultations.");

            // One feedback per consultation
            var alreadyExists = await _context.SessionFeedbacks
                .AnyAsync(f => f.ConsultationId == dto.ConsultationId);

            if (alreadyExists)
                throw new InvalidOperationException(
                    "Feedback has already been submitted for this consultation.");

            var feedback = new SessionFeedback
            {
                ConsultationId = dto.ConsultationId,
                PatientId      = patient.PatientId,
                Rating         = dto.Rating,
                Comment        = dto.Comment,
                CreatedAt      = DateTime.UtcNow,
                IsEdited       = false
            };

            _context.SessionFeedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            // Reload with navigations for the response DTO
            feedback.Patient      = patient;
            feedback.Consultation = consultation;

            return MapToDto(feedback);
        }

        // ?? UpdateFeedbackAsync ???????????????????????????????????????????????????

        public async Task<SessionFeedbackDto> UpdateFeedbackAsync(
            int feedbackId, UpdateFeedbackDto dto, int callerUserId)
        {
            var feedback = await LoadFeedbackAsync(feedbackId);

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == callerUserId && !p.IsDeleted)
                ?? throw new KeyNotFoundException("Patient profile not found for the authenticated user.");

            if (feedback.PatientId != patient.PatientId)
                throw new UnauthorizedAccessException("You can only edit your own feedback.");

            if (dto.Rating.HasValue)
                feedback.Rating = dto.Rating.Value;

            if (dto.Comment != null)
                feedback.Comment = dto.Comment;

            feedback.IsEdited  = true;
            feedback.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToDto(feedback);
        }

        // ?? GetFeedbackByConsultationAsync ????????????????????????????????????????

        public async Task<SessionFeedbackDto> GetFeedbackByConsultationAsync(
            int consultationId, int callerUserId, string callerRole)
        {
            var feedback = await _context.SessionFeedbacks
                .Include(f => f.Consultation)
                    .ThenInclude(c => c!.Appointment)
                        .ThenInclude(a => a!.Doctor)
                .Include(f => f.Patient)
                .FirstOrDefaultAsync(f => f.ConsultationId == consultationId)
                ?? throw new KeyNotFoundException(
                    $"No feedback found for consultation ID {consultationId}.");

            if (callerRole == "Patient")
            {
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == callerUserId && !p.IsDeleted);

                if (patient == null || feedback.PatientId != patient.PatientId)
                    throw new UnauthorizedAccessException(
                        "You can only view your own feedback.");
            }
            else if (callerRole == "Doctor")
            {
                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.UserId == callerUserId && !d.IsDeleted);

                var appointmentDoctorId = feedback.Consultation?.Appointment?.DoctorId;
                if (doctor == null || appointmentDoctorId != doctor.DoctorId)
                    throw new UnauthorizedAccessException(
                        "You can only view feedback for your own consultations.");
            }

            return MapToDto(feedback);
        }

        // ?? GetFeedbackByDoctorAsync ??????????????????????????????????????????????

        public async Task<IEnumerable<SessionFeedbackDto>> GetFeedbackByDoctorAsync(
            int doctorId, int callerUserId, string callerRole)
        {
            if (callerRole == "Doctor")
            {
                var callerDoctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.UserId == callerUserId && !d.IsDeleted);

                if (callerDoctor == null || callerDoctor.DoctorId != doctorId)
                    throw new UnauthorizedAccessException(
                        "You can only view feedback for your own consultations.");
            }

            var feedbacks = await _context.SessionFeedbacks
                .Include(f => f.Consultation)
                    .ThenInclude(c => c!.Appointment)
                        .ThenInclude(a => a!.Doctor)
                .Include(f => f.Patient)
                .Where(f => f.Consultation!.Appointment!.DoctorId == doctorId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return feedbacks.Select(MapToDto);
        }

        // ?? GetFeedbackByPatientAsync ?????????????????????????????????????????????

        public async Task<IEnumerable<SessionFeedbackDto>> GetFeedbackByPatientAsync(
            int patientId, int callerUserId, string callerRole)
        {
            if (callerRole == "Patient")
            {
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == callerUserId && !p.IsDeleted);

                if (patient == null || patient.PatientId != patientId)
                    throw new UnauthorizedAccessException(
                        "You can only view your own feedback.");
            }

            var feedbacks = await _context.SessionFeedbacks
                .Include(f => f.Consultation)
                    .ThenInclude(c => c!.Appointment)
                        .ThenInclude(a => a!.Doctor)
                .Include(f => f.Patient)
                .Where(f => f.PatientId == patientId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return feedbacks.Select(MapToDto);
        }

        // ?? DeleteFeedbackAsync ???????????????????????????????????????????????????

        public async Task DeleteFeedbackAsync(int feedbackId, int callerUserId, string callerRole)
        {
            var feedback = await _context.SessionFeedbacks
                .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId)
                ?? throw new KeyNotFoundException($"Feedback with ID {feedbackId} not found.");

            if (callerRole != "Admin")
            {
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == callerUserId && !p.IsDeleted);

                if (patient == null || feedback.PatientId != patient.PatientId)
                    throw new UnauthorizedAccessException(
                        "You can only delete your own feedback.");
            }

            _context.SessionFeedbacks.Remove(feedback);
            await _context.SaveChangesAsync();
        }
    }
}
