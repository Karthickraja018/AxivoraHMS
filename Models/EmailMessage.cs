namespace Axivora.Models
{
    /// <summary>
    /// Represents a single email message that is placed on the in-memory queue
    /// and later processed by <see cref="Services.BackgroundServices.EmailBackgroundService"/>.
    /// </summary>
    public class EmailMessage
    {
        /// <summary>Recipient email address.</summary>
        public string To { get; set; } = null!;

        /// <summary>Email subject line.</summary>
        public string Subject { get; set; } = null!;

        /// <summary>HTML body of the email.</summary>
        public string Body { get; set; } = null!;
    }
}
