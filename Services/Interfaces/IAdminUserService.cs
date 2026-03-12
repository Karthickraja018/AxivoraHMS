using Axivora.DTOs;
using Axivora.Helpers;

namespace Axivora.Services.Interfaces
{
    public interface IAdminUserService
    {
        Task<PaginationResponse<AdminUserDto>> GetAllUsersAsync(string? email, string? role, bool? isActive, PaginationParams paginationParams);
        Task<AdminUserDto> GetUserByIdAsync(int userId);
        Task<AdminUserDto> DisableUserAsync(int userId);
        Task<AdminUserDto> EnableUserAsync(int userId);
    }
}
