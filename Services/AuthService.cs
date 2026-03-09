using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
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
        private readonly IPasswordHasher _passwordHasher;

        public AuthService(
            AxivoraDbContext context,
            IConfiguration configuration,
            ITokenService tokenService,
            IPasswordHasher passwordHasher)
        {
            _context = context;
            _configuration = configuration;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
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
                PasswordHash = _passwordHasher.Hash(registerDto.Password), // Hash password
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

            _context.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = role.RoleId });
            await _context.SaveChangesAsync();

            // 5. TODO: Send verification email
            // await _emailService.SendVerificationEmailAsync(user.Email, verificationCode);

            // 6. Generate JWT token using token service
            var token = _tokenService.GenerateJwtToken(user.UserId, user.Email, registerDto.Role);

            // 7. Check if profile is completed
            bool profileCompleted = registerDto.Role == "Patient"
                ? await _context.Patients.AnyAsync(p => p.UserId == user.UserId)
                : registerDto.Role == "Doctor" && await _context.Doctors.AnyAsync(d => d.UserId == user.UserId);

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
            if (!_passwordHasher.Verify(loginDto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password.");

            // 3. Check if user is active
            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is disabled. Please contact support.");

            // 4. Get user role
            var roleName = user.UserRoles.FirstOrDefault()?.Role?.RoleName ?? "Patient";

            // 5. Generate JWT token using token service
            var token = _tokenService.GenerateJwtToken(user.UserId, user.Email, roleName);

            // 6. Check if profile is completed
            bool profileCompleted = roleName == "Patient"
                ? await _context.Patients.AnyAsync(p => p.UserId == user.UserId)
                : roleName == "Doctor" && await _context.Doctors.AnyAsync(d => d.UserId == user.UserId);

            // 7. Update last login (optional)
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                UserId = user.UserId,
                Email = user.Email,
                Token = token,
                Role = roleName,
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

            var resetToken = GenerateSecureToken();
            Console.WriteLine($"Password reset token for {email}: {resetToken}");
        }

        public async Task<bool> ResetPasswordAsync(string email, string resetToken, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            // Update password
            user.PasswordHash = _passwordHasher.Hash(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        private static string GenerateSecureToken()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
