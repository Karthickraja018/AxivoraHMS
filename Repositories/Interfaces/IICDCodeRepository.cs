using Axivora.Models;

namespace Axivora.Repositories.Interfaces
{
    public interface IICDCodeRepository
    {
        Task<int> CountAsync(string? code, string? description);
        Task<IEnumerable<ICDCode>> GetPagedAsync(string? code, string? description, int skip, int take);
    }
}
