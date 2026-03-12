using Axivora.Models;

namespace Axivora.Repositories.Interfaces
{
    public interface IAdminUserRepository
    {
        Task<int> CountAsync(string? email, string? role, bool? isActive);
        Task<IEnumerable<User>> GetPagedAsync(string? email, string? role, bool? isActive, int skip, int take);
        Task<User?> GetByIdAsync(int userId);
        Task SaveChangesAsync();
    }
}
