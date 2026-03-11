using Axivora.DTOs;

namespace Axivora.Services.Interfaces
{
    public interface IFeedbackService
    {
        /// <summary>
        /// Submits feedback for a completed consultation.
        /// Only the patient who attended the consultation may call this.
        /// Throws <see cref="KeyNotFoundException"/> if the consultation does not exist.
        /// Throws <see cref="UnauthorizedAccessException"/> if the caller is not the consultation's patient.
        /// Throws <see cref="InvalidOperationException"/> if the consultation is not yet completed
        /// or if feedback has already been submitted.
        /// </summary>
        Task<SessionFeedbackDto> CreateFeedbackAsync(CreateFeedbackDto dto, int callerUserId);

        /// <summary>
        /// Updates the rating and/or comment of existing feedback.
        /// Only the original submitting patient may edit their feedback.
        /// </summary>
        Task<SessionFeedbackDto> UpdateFeedbackAsync(int feedbackId, UpdateFeedbackDto dto, int callerUserId);

        /// <summary>
        /// Returns the feedback for a specific consultation.
        /// Patients may only retrieve their own feedback.
        /// Doctors may only retrieve feedback for their own consultations.
        /// Admins are unrestricted.
        /// </summary>
        Task<SessionFeedbackDto> GetFeedbackByConsultationAsync(int consultationId, int callerUserId, string callerRole);

        /// <summary>
        /// Returns all feedback submitted for a doctor's consultations, visible to Admin and that doctor.
        /// </summary>
        Task<IEnumerable<SessionFeedbackDto>> GetFeedbackByDoctorAsync(int doctorId, int callerUserId, string callerRole);

        /// <summary>
        /// Returns all feedback submitted by a patient, visible to Admin and that patient.
        /// </summary>
        Task<IEnumerable<SessionFeedbackDto>> GetFeedbackByPatientAsync(int patientId, int callerUserId, string callerRole);

        /// <summary>
        /// Deletes feedback. Only the submitting patient or an Admin may delete.
        /// </summary>
        Task DeleteFeedbackAsync(int feedbackId, int callerUserId, string callerRole);
    }
}
