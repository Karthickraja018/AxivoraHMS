using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

namespace Axivora.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AxivoraDbContext _context;

        public AuthRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        public async Task<bool> EmailExistsAsync(string email) =>
            await _context.Users.AnyAsync(u => u.Email == email);

        public async Task<User?> GetUserByEmailAsync(string email) =>
            await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

        public async Task<User?> GetUserByIdAsync(int userId) =>
            await _context.Users.FindAsync(userId);

        public async Task SaveOtpAsync(int userId, string hashedOtp, DateTime expiresAt)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user is null) return;
            user.EmailVerificationOtp = hashedOtp;
            user.OtpExpiresAt         = expiresAt;
            user.UpdatedAt            = DateTime.UtcNow;
        }

        public async Task<Role?> GetRoleByNameAsync(string roleName) =>
            await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);

        public async Task AddUserAsync(User user) =>
            await _context.Users.AddAsync(user);

        public async Task AddRoleAsync(Role role) =>
            await _context.Roles.AddAsync(role);

        public async Task AddUserRoleAsync(UserRole userRole) =>
            await _context.UserRoles.AddAsync(userRole);

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token) =>
            await _context.RefreshTokens
                .Include(rt => rt.User)
                    .ThenInclude(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(rt => rt.Token == token);

        public async Task AddRefreshTokenAsync(RefreshToken refreshToken) =>
            await _context.RefreshTokens.AddAsync(refreshToken);

        public async Task<bool> PatientProfileExistsAsync(int userId) =>
            await _context.Patients.AnyAsync(p => p.UserId == userId);

        public async Task<bool> DoctorProfileExistsAsync(int userId) =>
            await _context.Doctors.AnyAsync(d => d.UserId == userId);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}
