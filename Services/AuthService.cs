using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using Axivora.Data;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;
using Axivora.Security;

namespace Axivora.Services
{
    public class AuthService : IAuthService
    {
        private readonly AxivoraDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ITokenService _tokenService;

        public AuthService(AxivoraDbContext context, IConfiguration configuration, ITokenService tokenService)
        {
            _context = context;
            _configuration = configuration;
            _tokenService = tokenService;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto registerDto)
        {
            // 1. Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
                throw new InvalidOperationException("Email already registered.");

            // 2. Validate role - Only allow Patient self-registration
            // Admin and Doctor roles should be created through admin endpoints
            if (registerDto.Role != "Patient")
                throw new InvalidOperationException("Self-registration is only allowed for Patient role. Admins and Doctors must be created by system administrators.");

            // 3. Create user
            var user = new User
            {
                Email = registerDto.Email,
                PasswordHash = HashPassword(registerDto.Password), // Hash password
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 4. Create role assignment
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == registerDto.Role);
            if (role == null)
            {
                // Create role if it doesn't exist
                role = new Role { RoleName = registerDto.Role };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
            }

            var userRole = new UserRole
            {
                UserId = user.UserId,
                RoleId = role.RoleId
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            // 5. TODO: Send verification email
            // await _emailService.SendVerificationEmailAsync(user.Email, verificationCode);

            // 6. Generate JWT token using token service
            var token = _tokenService.GenerateJwtToken(user.UserId, user.Email, registerDto.Role);

            // 7. Check if profile is completed
            bool profileCompleted = false;
            if (registerDto.Role == "Patient")
            {
                profileCompleted = await _context.Patients.AnyAsync(p => p.UserId == user.UserId);
            }
            else if (registerDto.Role == "Doctor")
            {
                profileCompleted = await _context.Doctors.AnyAsync(d => d.UserId == user.UserId);
            }

            return new AuthResponseDto
            {
                UserId = user.UserId,
                Email = user.Email,
                Token = token,
                Role = registerDto.Role,
                EmailVerified = false, // TODO: implement email verification
                ProfileCompleted = profileCompleted
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            // 1. Find user by email
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null || user.IsDeleted)
                throw new UnauthorizedAccessException("Invalid email or password.");

            // 2. Verify password
            if (!VerifyPassword(loginDto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password.");

            // 3. Check if user is active
            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is disabled. Please contact support.");

            // 4. Get user role
            var userRole = user.UserRoles.FirstOrDefault();
            var role = userRole?.Role?.RoleName ?? "Patient";

            // 5. Generate JWT token using token service
            var token = _tokenService.GenerateJwtToken(user.UserId, user.Email, role);

            // 6. Check if profile is completed
            bool profileCompleted = false;
            if (role == "Patient")
            {
                profileCompleted = await _context.Patients.AnyAsync(p => p.UserId == user.UserId);
            }
            else if (role == "Doctor")
            {
                profileCompleted = await _context.Doctors.AnyAsync(d => d.UserId == user.UserId);
            }

            // 7. Update last login (optional)
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                UserId = user.UserId,
                Email = user.Email,
                Token = token,
                Role = role,
                EmailVerified = true, // TODO: implement email verification
                ProfileCompleted = profileCompleted
            };
        }

        public async Task<bool> VerifyEmailAsync(string email, string verificationCode)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            // Placeholder implementation
            return await Task.FromResult(true);
        }

        public async Task SendPasswordResetTokenAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            var resetToken = GenerateResetToken();
            Console.WriteLine($"Password reset token for {email}: {resetToken}");
        }

        public async Task<bool> ResetPasswordAsync(string email, string resetToken, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            // Update password
            user.PasswordHash = HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        #region Helper Methods

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == passwordHash;
        }

        private string GenerateResetToken()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        #endregion
    }
}
