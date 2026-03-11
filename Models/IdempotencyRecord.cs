namespace Axivora.Models
{
    /// <summary>
    /// Stores processed idempotency keys to prevent duplicate appointment bookings
    /// caused by network retries. Checked only on POST /appointments.
    /// </summary>
    public class IdempotencyRecord
    {
        public int Id { get; set; }

        /// <summary>The Idempotency-Key header value supplied by the client.</summary>
        public string IdempotencyKey { get; set; } = null!;

        /// <summary>SHA-256 hash of the serialised request body for extra collision detection.</summary>
        public string RequestHash { get; set; } = null!;

        /// <summary>The serialised JSON response payload returned for this request.</summary>
        public string ResponsePayload { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}
