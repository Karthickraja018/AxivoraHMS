using Axivora.Models;
using Axivora.Services.Interfaces;

namespace Axivora.Infrastructure.Email
{
    /// <summary>
    /// Implements <see cref="IEmailService"/> by loading HTML templates from disk,
    /// substituting placeholders, and placing the resulting <see cref="EmailMessage"/>
    /// on the <see cref="IEmailQueue"/>.
    ///
    /// No SMTP I/O occurs on the calling thread; the
    /// <see cref="Services.BackgroundServices.EmailBackgroundService"/> drains the queue
    /// and delivers messages via <see cref="SmtpEmailService"/>.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IEmailQueue _queue;
        private readonly ILogger<EmailService> _logger;

        // Templates directory is resolved relative to the application base directory
        private readonly string _templateDirectory;

        public EmailService(IEmailQueue queue, ILogger<EmailService> logger)
        {
            _queue   = queue;
            _logger  = logger;
            _templateDirectory = Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Email", "Templates");
        }

        // IEmailService

        public Task SendEmailVerificationOtpAsync(string email, string otp)
        {
            var body = LoadTemplate("EmailVerificationOtp.html")
                .Replace("{OTP}", otp);

            Enqueue(email, "Email Verification � AxivoraHMS", body);
            return Task.CompletedTask;
        }

        public Task SendForgotPasswordEmailAsync(string email, string resetLink)
        {
            var body = LoadTemplate("ForgotPassword.html")
                .Replace("{ResetLink}", resetLink);

            Enqueue(email, "Password Reset Request � AxivoraHMS", body);
            return Task.CompletedTask;
        }

        public Task SendDoctorAccountCreatedAsync(string email, string doctorName, string tempPassword)
        {
            var body = LoadTemplate("DoctorAccountCreated.html")
                .Replace("{DoctorName}", doctorName)
                .Replace("{TempPassword}", tempPassword)
                .Replace("{Email}", email);

            Enqueue(email, "Welcome to AxivoraHMS � Your Account is Ready", body);
            return Task.CompletedTask;
        }

        public Task SendAppointmentRequestReceivedAsync(
            string email, string patientName, string doctorName, DateTime appointmentTime)
        {
            var body = LoadTemplate("AppointmentRequestReceived.html")
                .Replace("{PatientName}", patientName)
                .Replace("{DoctorName}", doctorName)
                .Replace("{AppointmentTime}", FormatTime(appointmentTime));

            Enqueue(email, "Appointment request received � AxivoraHMS", body);
            return Task.CompletedTask;
        }

        public Task SendAppointmentConfirmationAsync(
            string email, string patientName, string doctorName, DateTime appointmentTime)
        {
            var body = LoadTemplate("AppointmentConfirmation.html")
                .Replace("{PatientName}", patientName)
                .Replace("{DoctorName}", doctorName)
                .Replace("{AppointmentTime}", FormatTime(appointmentTime));

            Enqueue(email, "Appointment Confirmed � AxivoraHMS", body);
            return Task.CompletedTask;
        }

        public Task SendAppointmentReminderAsync(
            string email, string patientName, string doctorName, DateTime appointmentTime)
        {
            var body = LoadTemplate("AppointmentReminder.html")
                .Replace("{PatientName}", patientName)
                .Replace("{DoctorName}", doctorName)
                .Replace("{AppointmentTime}", FormatTime(appointmentTime));

            Enqueue(email, "Appointment Reminder � Tomorrow at AxivoraHMS", body);
            return Task.CompletedTask;
        }

        public Task SendAppointmentCompletedAsync(
            string email, string patientName, string doctorName, DateTime appointmentTime)
        {
            var body = LoadTemplate("AppointmentCompleted.html")
                .Replace("{PatientName}", patientName)
                .Replace("{DoctorName}", doctorName)
                .Replace("{AppointmentTime}", FormatTime(appointmentTime));

            Enqueue(email, "Consultation Completed � AxivoraHMS", body);
            return Task.CompletedTask;
        }

        // Private helpers

        private void Enqueue(string to, string subject, string body)
        {
            _queue.Enqueue(new EmailMessage { To = to, Subject = subject, Body = body });
            _logger.LogDebug("Email queued to {To} | Subject: {Subject}", to, subject);
        }

        /// <summary>
        /// Reads the named template file from the Templates directory.
        /// Returns a minimal fallback string if the file is missing so no exception
        /// is thrown during development or when templates are not yet deployed.
        /// </summary>
        private string LoadTemplate(string fileName)
        {
            var path = Path.Combine(_templateDirectory, fileName);

            if (!File.Exists(path))
            {
                _logger.LogWarning("Email template '{File}' not found at {Path}.", fileName, path);
                return $"<p>{fileName.Replace(".html", string.Empty)}</p>";
            }

            return File.ReadAllText(path);
        }

        private static string FormatTime(DateTime dt) =>
            dt.ToString("dddd, dd MMMM yyyy 'at' HH:mm");
    }
}
