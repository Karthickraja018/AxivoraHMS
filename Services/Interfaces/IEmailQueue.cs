using Axivora.Models;

namespace Axivora.Services.Interfaces
{
    /// <summary>
    /// Defines the in-memory email queue used to decouple email sending from request handling.
    /// Services enqueue messages; <see cref="BackgroundServices.EmailBackgroundService"/> dequeues and sends them.
    /// </summary>
    public interface IEmailQueue
    {
        /// <summary>Adds an email message to the end of the queue.</summary>
        void Enqueue(EmailMessage message);

        /// <summary>
        /// Attempts to remove and return the next email message from the queue.
        /// Returns <c>null</c> when the queue is empty.
        /// </summary>
        EmailMessage? Dequeue();
    }
}
