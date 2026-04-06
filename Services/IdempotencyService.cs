using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.Models;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    /// <summary>
    /// Provides idempotency for POST /appointments by storing the result of the first
    /// successful booking against the client-supplied Idempotency-Key header.
    ///
    /// On a retry the stored response is returned unchanged, so the client receives an
    /// identical result without a duplicate appointment being created.
    /// </summary>
    public class IdempotencyService : IIdempotencyService
    {
        private readonly AxivoraDbContext _context;

        public IdempotencyService(AxivoraDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns the previously stored response payload for the given key, or null if
        /// this is the first request with this key.
        /// </summary>
        public async Task<string?> GetStoredResponseAsync(string idempotencyKey)
        {
            var record = await _context.IdempotencyRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey);

            return record?.ResponsePayload;
        }

        /// <summary>
        /// Persists the response payload so future retries return the same result.
        /// If a record already exists for this key it is left unchanged (idempotent).
        /// </summary>
        public async Task StoreResponseAsync(string idempotencyKey, string requestBody, object response)
        {
            // Avoid inserting a duplicate if a concurrent request already stored a record
            var exists = await _context.IdempotencyRecords
                .AsNoTracking()
                .AnyAsync(r => r.IdempotencyKey == idempotencyKey);

            if (exists)
                return;

            var record = new IdempotencyRecord
            {
                IdempotencyKey  = idempotencyKey,
                RequestHash     = ComputeSha256(requestBody),
                ResponsePayload = JsonSerializer.Serialize(response,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                CreatedAt       = DateTime.UtcNow
            };

            _context.IdempotencyRecords.Add(record);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Another concurrent request inserted the same key between our check and insert
                // � safe to swallow; the first writer wins
            }
        }

        /// <summary>Computes a SHA-256 hex digest of the given string.</summary>
        private static string ComputeSha256(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
