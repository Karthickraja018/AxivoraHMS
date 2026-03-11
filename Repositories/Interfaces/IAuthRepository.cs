using Axivora.Models;

namespace Axivora.Repositories.Interfaces
{
    public interface IAuthRepository
    {
        Task<bool> EmailExistsAsync(string email);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int userId);
        Task<Role?> GetRoleByNameAsync(string roleName);
        Task AddUserAsync(User user);
        Task AddRoleAsync(Role role);
        Task AddUserRoleAsync(UserRole userRole);
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        Task AddRefreshTokenAsync(RefreshToken refreshToken);
        Task<bool> PatientProfileExistsAsync(int userId);
        Task<bool> DoctorProfileExistsAsync(int userId);
        Task SaveChangesAsync();
    }
}
