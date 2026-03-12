using Axivora.Models;

namespace Axivora.Repositories.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<IEnumerable<Department>> GetAllActiveAsync();
        Task<int> CountActiveAsync();
        Task<IEnumerable<Department>> GetPagedAsync(int skip, int take);
        Task<Department?> GetByIdAsync(int departmentId);
        Task<bool> NameExistsAsync(string name, int? excludeId = null);
        Task AddAsync(Department department);
        Task SaveChangesAsync();
    }
}
