using Axivora.DTOs;
using Axivora.Helpers;
using Axivora.Repositories.Interfaces;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    public class AdminUserService : IAdminUserService
    {
        private readonly IAdminUserRepository _repository;

        public AdminUserService(IAdminUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<PaginationResponse<AdminUserDto>> GetAllUsersAsync(
            string? email,
            string? role,
            bool? isActive,
            PaginationParams paginationParams)
        {
            var totalCount = await _repository.CountAsync(email, role, isActive);
            var users      = await _repository.GetPagedAsync(
                email,
                role,
                isActive,
                (paginationParams.PageNumber - 1) * paginationParams.PageSize,
                paginationParams.PageSize);

            var dtos = users.Select(u => new AdminUserDto
            {
                Id        = u.UserId,
                Email     = u.Email,
                Role      = u.UserRoles.FirstOrDefault()?.Role?.RoleName,
                IsActive  = u.IsActive,
                CreatedAt = u.CreatedAt
            });

            return new PaginationResponse<AdminUserDto>(dtos, totalCount, paginationParams.PageNumber, paginationParams.PageSize);
        }

        public async Task<AdminUserDto> GetUserByIdAsync(int userId)
        {
            var user = await _repository.GetByIdAsync(userId);

            if (user is null)
                throw new KeyNotFoundException($"User with ID {userId} not found.");

            return new AdminUserDto
            {
                Id        = user.UserId,
                Email     = user.Email,
                Role      = user.UserRoles.FirstOrDefault()?.Role?.RoleName,
                IsActive  = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<AdminUserDto> DisableUserAsync(int userId)
        {
            var user = await _repository.GetByIdAsync(userId);

            if (user is null)
                throw new KeyNotFoundException($"User with ID {userId} not found.");

            user.IsActive  = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync();

            return new AdminUserDto
            {
                Id        = user.UserId,
                Email     = user.Email,
                Role      = user.UserRoles.FirstOrDefault()?.Role?.RoleName,
                IsActive  = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<AdminUserDto> EnableUserAsync(int userId)
        {
            var user = await _repository.GetByIdAsync(userId);

            if (user is null)
                throw new KeyNotFoundException($"User with ID {userId} not found.");

            user.IsActive  = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync();

            return new AdminUserDto
            {
                Id        = user.UserId,
                Email     = user.Email,
                Role      = user.UserRoles.FirstOrDefault()?.Role?.RoleName,
                IsActive  = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
