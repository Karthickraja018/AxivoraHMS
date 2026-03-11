using Axivora.Models;

namespace Axivora.Repositories.Interfaces
{
    public interface IMedicineRepository
    {
        Task<int> CountAsync(string? search);
        Task<IEnumerable<Medicine>> GetPagedAsync(string? search, int skip, int take);
        Task<Medicine?> GetByIdAsync(int id);
    }
}
