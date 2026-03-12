using Axivora.DTOs;
using Axivora.Helpers;

namespace Axivora.Services.Interfaces
{
    public interface IDepartmentService
    {
        Task<PaginationResponse<DepartmentDto>> GetAllAsync(PaginationParams paginationParams);
        Task<DepartmentDto> GetByIdAsync(int departmentId);
        Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto);
        Task<DepartmentDto> UpdateAsync(int departmentId, UpdateDepartmentDto dto);
        Task DeleteAsync(int departmentId);
    }
}
