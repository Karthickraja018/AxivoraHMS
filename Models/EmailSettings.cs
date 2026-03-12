namespace Axivora.Models
{
    /// <summary>
    /// Strongly-typed SMTP configuration bound from the "EmailSettings" section of appsettings.json.
    /// Injected via IOptions&lt;EmailSettings&gt; in the Infrastructure layer.
    /// </summary>
    public class EmailSettings
    {
        public string Host { get; set; } = null!;
        public int Port { get; set; }
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FromEmail { get; set; } = null!;
    }
}
