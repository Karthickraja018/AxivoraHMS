namespace Axivora.Services.Interfaces
{
    public interface IIdempotencyService
    {
        Task<string?> GetStoredResponseAsync(string idempotencyKey);
        Task StoreResponseAsync(string idempotencyKey, string requestBody, object response);
    }
}
