using System.Collections.Concurrent;
using Axivora.Models;
using Axivora.Services.Interfaces;

namespace Axivora.Infrastructure.Email
{
    /// <summary>
    /// Thread-safe in-memory email queue backed by <see cref="ConcurrentQueue{T}"/>.
    /// Registered as a singleton so the same queue instance is shared across all
    /// scoped services and the background worker.
    /// </summary>
    public class EmailQueue : IEmailQueue
    {
        private readonly ConcurrentQueue<EmailMessage> _queue = new();

        /// <inheritdoc/>
        public void Enqueue(EmailMessage message) => _queue.Enqueue(message);

        /// <inheritdoc/>
        public EmailMessage? Dequeue() =>
            _queue.TryDequeue(out var message) ? message : null;
    }
}
