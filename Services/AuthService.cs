using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Axivora.Services
{
    public class AuthService : IAuthService
    {
        private readonly AxivoraDbContext _context;
        // TODO: Inject IConfiguration for JWT secret key
        // TODO: Inject IEmailService for sending verification/reset emails

        public AuthService(AxivoraDbContext context)
        {
            _context = context;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto registerDto)
        {
            // 1. Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
                throw new InvalidOperationException("Email already registered.");

            // 2. Validate role
            if (registerDto.Role != "Patient" && registerDto.Role != "Doctor")
                throw new InvalidOperationException("Invalid role. Must be 'Patient' or 'Doctor'.");

            // 3. Create user
            var user = new User
            {
                Email = registerDto.Email,
                Password = HashPassword(registerDto.Password), // Hash password
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

            // 6. Generate JWT token (TODO: implement proper JWT)
            var token = GenerateTemporaryToken(user.UserId, registerDto.Role);

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
            if (!VerifyPassword(loginDto.Password, user.Password))
                throw new UnauthorizedAccessException("Invalid email or password.");

            // 3. Check if user is active
            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is disabled. Please contact support.");

            // 4. Get user role
            var userRole = user.UserRoles.FirstOrDefault();
            var role = userRole?.Role?.RoleName ?? "Patient";

            // 5. Generate JWT token (TODO: implement proper JWT)
            var token = GenerateTemporaryToken(user.UserId, role);

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
            // TODO: Implement email verification logic
            // 1. Find user by email
            // 2. Compare verification code (stored in database or cache)
            // 3. Update user.EmailVerified = true
            // 4. Return true if successful

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

            // TODO: Generate reset token and store in database with expiration
            var resetToken = GenerateResetToken();
            
            // TODO: Send email with reset link
            // await _emailService.SendPasswordResetEmailAsync(email, resetToken);

            // For now, just log it (remove in production)
            Console.WriteLine($"Password reset token for {email}: {resetToken}");
        }

        public async Task<bool> ResetPasswordAsync(string email, string resetToken, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            // TODO: Verify reset token from database/cache
            // For now, accept any token (INSECURE - implement properly)

            // Update password
            user.Password = HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            return true;
        }

        #region Helper Methods

        /// <summary>
        /// Hash password using SHA256 (TODO: Replace with BCrypt.Net)
        /// </summary>
        private string HashPassword(string password)
        {
            // TEMPORARY: Use SHA256 (NOT RECOMMENDED for production)
            // TODO: Install BCrypt.Net-Next and use:
            // return BCrypt.Net.BCrypt.HashPassword(password);
            
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Verify password hash
        /// </summary>
        private bool VerifyPassword(string password, string passwordHash)
        {
            // TEMPORARY: Compare SHA256 hashes
            // TODO: Use BCrypt verification:
            // return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            
            var hashOfInput = HashPassword(password);
            return hashOfInput == passwordHash;
        }

        /// <summary>
        /// Generate temporary token (TODO: Replace with JWT)
        /// </summary>
        private string GenerateTemporaryToken(int userId, string role)
        {
            // TEMPORARY: Simple base64 token (INSECURE)
            // TODO: Install Microsoft.AspNetCore.Authentication.JwtBearer
            // and implement proper JWT token generation with secret key, expiration, etc.
            
            var tokenData = $"{userId}:{role}:{DateTime.UtcNow.Ticks}";
            var bytes = Encoding.UTF8.GetBytes(tokenData);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Generate password reset token
        /// </summary>
        private string GenerateResetToken()
        {
            // Generate a secure random token
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        #endregion
    }
}
