namespace Axivora.Services.Interfaces
{
    /// <summary>
    /// Application-layer contract for all outbound email operations.
    /// Implementations (e.g. SmtpEmailService) live in the Infrastructure layer.
    /// Services call these methods to compose and deliver email messages.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>Sends an OTP code to the given address for email verification.</summary>
        Task SendEmailVerificationOtpAsync(string email, string otp);

        /// <summary>Sends a password-reset link to the user.</summary>
        Task SendForgotPasswordEmailAsync(string email, string resetLink);

        /// <summary>Sends the new doctor their welcome message and temporary credentials.</summary>
        Task SendDoctorAccountCreatedAsync(string email, string doctorName, string tempPassword);

        /// <summary>Notifies the patient that their appointment has been scheduled with date and time details.</summary>
        Task SendAppointmentScheduledAsync(string email, string patientName, string doctorName, DateTime appointmentTime);

        /// <summary>Sends a 24-hour reminder before the scheduled appointment.</summary>
        Task SendAppointmentReminderAsync(string email, string patientName, string doctorName, DateTime appointmentTime);

        /// <summary>Sends a 2-hour reminder before the scheduled appointment.</summary>
        Task SendAppointmentReminder2HoursAsync(string email, string patientName, string doctorName, DateTime appointmentTime);

        /// <summary>Notifies the patient that their consultation has been completed.</summary>
        Task SendAppointmentCompletedAsync(string email, string patientName, string doctorName, DateTime appointmentTime);

        /// <summary>Notifies the patient that a lab result/report has been uploaded.</summary>
        Task SendLabResultUploadedAsync(string email, string patientName, string doctorName, string testName, DateTime resultAt);
    }
}
