using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;
using Axivora.Security;
using Axivora.Repositories.Interfaces;

namespace Axivora.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;

        // OTP is valid for 10 minutes
        private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);

        // Minimum gap before a new OTP may be issued (prevents spam)
        private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);

        public AuthService(
            IAuthRepository repository,
            IConfiguration configuration,
            ITokenService tokenService,
            IPasswordHasher passwordHasher,
            IEmailService emailService)
        {
            _repository    = repository;
            _configuration = configuration;
            _tokenService  = tokenService;
            _passwordHasher = passwordHasher;
            _emailService  = emailService;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto registerDto)
        {
            // 1. Check if user already exists
            if (await _repository.EmailExistsAsync(registerDto.Email))
                throw new InvalidOperationException("Email already registered.");

            // 2. Validate role - Only allow Patient self-registration
            // Admin and Doctor roles should be created through admin endpoints
            if (registerDto.Role != "Patient")
                throw new InvalidOperationException("Self-registration is only allowed for Patient role. Admins and Doctors must be created by system administrators.");

            // 3. Create user
            var user = new User
            {
                Email           = registerDto.Email,
                PasswordHash    = _passwordHasher.Hash(registerDto.Password),
                IsActive        = true,
                IsDeleted       = false,
                IsEmailVerified = false,
                CreatedAt       = DateTime.UtcNow,
                UpdatedAt       = DateTime.UtcNow
            };

            await _repository.AddUserAsync(user);
            await _repository.SaveChangesAsync();

            // 4. Create role assignment
            var role = await _repository.GetRoleByNameAsync(registerDto.Role);
            if (role == null)
            {
                // Create role if it doesn't exist
                role = new Role { RoleName = registerDto.Role };
                await _repository.AddRoleAsync(role);
                await _repository.SaveChangesAsync();
            }

            await _repository.AddUserRoleAsync(new UserRole { UserId = user.UserId, RoleId = role.RoleId });
            await _repository.SaveChangesAsync();

            // Generate OTP, hash it for storage, then enqueue the email
            var otp       = GenerateOtp();
            var expiresAt = DateTime.UtcNow.Add(OtpLifetime);
            await _repository.SaveOtpAsync(user.UserId, _passwordHasher.Hash(otp), expiresAt);
            await _repository.SaveChangesAsync();
            await _emailService.SendEmailVerificationOtpAsync(user.Email, otp);

            // 6. Generate JWT token using token service
            var token        = _tokenService.GenerateJwtToken(user.UserId, user.Email, registerDto.Role);
            var refreshToken = await CreateRefreshTokenAsync(user.UserId);

            // 7. Check if profile is completed
            bool profileCompleted = registerDto.Role == "Patient"
                ? await _repository.PatientProfileExistsAsync(user.UserId)
                : registerDto.Role == "Doctor" && await _repository.DoctorProfileExistsAsync(user.UserId);

            return new AuthResponseDto
            {
                UserId           = user.UserId,
                Email            = user.Email,
                Token            = token,
                RefreshToken     = refreshToken,
                TokenExpiresAt   = _tokenService.GetJwtExpiryTime(),
                Role             = registerDto.Role,
                EmailVerified    = false,
                ProfileCompleted = profileCompleted
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            // 1. Find user by email
            var user = await _repository.GetUserByEmailAsync(loginDto.Email);

            if (user == null || user.IsDeleted)
                throw new UnauthorizedAccessException("Invalid email or password.");

            // 2. Verify password
            if (!_passwordHasher.Verify(loginDto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password.");

            // 3. Check if user is active
            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is disabled. Please contact support.");

            // 4. Get user role
            var roleName     = user.UserRoles.FirstOrDefault()?.Role?.RoleName ?? "Patient";
            var token        = _tokenService.GenerateJwtToken(user.UserId, user.Email, roleName);
            var refreshToken = await CreateRefreshTokenAsync(user.UserId);

            // 5. Check if profile is completed
            bool profileCompleted = roleName == "Patient"
                ? await _repository.PatientProfileExistsAsync(user.UserId)
                : roleName == "Doctor" && await _repository.DoctorProfileExistsAsync(user.UserId);

            // 6. Update last login (optional)
            user.UpdatedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync();

            return new AuthResponseDto
            {
                UserId           = user.UserId,
                Email            = user.Email,
                Token            = token,
                RefreshToken     = refreshToken,
                TokenExpiresAt   = _tokenService.GetJwtExpiryTime(),
                Role             = roleName,
                EmailVerified    = user.IsEmailVerified,
                ProfileCompleted = profileCompleted
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _repository.GetRefreshTokenAsync(refreshToken);

            if (storedToken == null)
                throw new UnauthorizedAccessException("Invalid refresh token.");

            if (storedToken.IsRevoked)
                throw new UnauthorizedAccessException("Refresh token has been revoked.");

            if (storedToken.ExpiresAt < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token has expired.");

            var user = storedToken.User;

            if (!user.IsActive || user.IsDeleted)
                throw new UnauthorizedAccessException("Account is disabled.");

            // Rotate refresh token: revoke old, issue new
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;

            var roleName        = user.UserRoles.FirstOrDefault()?.Role?.RoleName ?? "Patient";
            var newJwtToken     = _tokenService.GenerateJwtToken(user.UserId, user.Email, roleName);
            var newRefreshToken = await CreateRefreshTokenAsync(user.UserId);

            bool profileCompleted = roleName == "Patient"
                ? await _repository.PatientProfileExistsAsync(user.UserId)
                : roleName == "Doctor" && await _repository.DoctorProfileExistsAsync(user.UserId);

            await _repository.SaveChangesAsync();

            return new AuthResponseDto
            {
                UserId           = user.UserId,
                Email            = user.Email,
                Token            = newJwtToken,
                RefreshToken     = newRefreshToken,
                TokenExpiresAt   = _tokenService.GetJwtExpiryTime(),
                Role             = roleName,
                EmailVerified    = user.IsEmailVerified,
                ProfileCompleted = profileCompleted
            };
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken, int callerUserId)
        {
            var storedToken = await _repository.GetRefreshTokenAsync(refreshToken);

            if (storedToken == null || storedToken.IsRevoked)
                return false;

            if (storedToken.UserId != callerUserId)
                throw new UnauthorizedAccessException("You can only revoke your own refresh tokens.");

            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Verifies the 6-digit OTP submitted by the user.
        /// On success, marks the user's email as verified and clears the OTP.
        /// Throws <see cref="InvalidOperationException"/> when the OTP is wrong or expired.
        /// </summary>
        public async Task VerifyEmailOtpAsync(string email, string otp)
        {
            var user = await _repository.GetUserByEmailAsync(email)
                ?? throw new KeyNotFoundException("User not found.");

            if (user.IsEmailVerified)
                throw new InvalidOperationException("Email is already verified.");

            if (user.EmailVerificationOtp is null || user.OtpExpiresAt is null)
                throw new InvalidOperationException("No verification OTP found. Please request a new one.");

            if (DateTime.UtcNow > user.OtpExpiresAt)
                throw new InvalidOperationException("OTP has expired. Please request a new one.");

            if (!_passwordHasher.Verify(otp, user.EmailVerificationOtp))
                throw new InvalidOperationException("Invalid OTP.");

            // Mark email as verified and clear the OTP fields
            user.IsEmailVerified      = true;
            user.EmailVerificationOtp = null;
            user.OtpExpiresAt         = null;
            user.UpdatedAt            = DateTime.UtcNow;
            await _repository.SaveChangesAsync();
        }

        /// <summary>
        /// Issues a fresh OTP and re-sends the verification email.
        /// Enforces a cooldown to prevent abuse: a new OTP cannot be requested
        /// if the current one was issued less than <see cref="ResendCooldown"/> ago.
        /// </summary>
        public async Task ResendEmailVerificationOtpAsync(string email)
        {
            var user = await _repository.GetUserByEmailAsync(email)
                ?? throw new KeyNotFoundException("User not found.");

            if (user.IsEmailVerified)
                throw new InvalidOperationException("Email is already verified.");

            // Enforce cooldown: OtpExpiresAt was set to UtcNow + 10 min on issue,
            // so "time remaining > (OtpLifetime - ResendCooldown)" means it was issued too recently.
            if (user.OtpExpiresAt.HasValue)
            {
                var issuedAt      = user.OtpExpiresAt.Value - OtpLifetime;
                var cooldownEndsAt = issuedAt + ResendCooldown;
                if (DateTime.UtcNow < cooldownEndsAt)
                {
                    var wait = (cooldownEndsAt - DateTime.UtcNow).Seconds;
                    throw new InvalidOperationException(
                        $"Please wait {wait} second(s) before requesting a new OTP.");
                }
            }

            var otp       = GenerateOtp();
            var expiresAt = DateTime.UtcNow.Add(OtpLifetime);
            await _repository.SaveOtpAsync(user.UserId, _passwordHasher.Hash(otp), expiresAt);
            await _repository.SaveChangesAsync();
            await _emailService.SendEmailVerificationOtpAsync(email, otp);
        }

        public async Task SendPasswordResetTokenAsync(string email)
        {
            var user = await _repository.GetUserByEmailAsync(email)
                ?? throw new KeyNotFoundException("User not found.");

            var resetToken = GenerateSecureToken();
            var baseUrl    = _configuration["AppSettings:BaseUrl"] ?? "https://axivora.health";
            var resetLink  = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(email)}";

            await _emailService.SendForgotPasswordEmailAsync(email, resetLink);
        }

        public async Task<bool> ResetPasswordAsync(string email, string resetToken, string newPassword)
        {
            var user = await _repository.GetUserByEmailAsync(email)
                ?? throw new KeyNotFoundException("User not found.");

            user.PasswordHash = _passwordHasher.Hash(newPassword);
            user.UpdatedAt    = DateTime.UtcNow;
            await _repository.SaveChangesAsync();

            return true;
        }

        // ?? Private helpers ??????????????????????????????????????????????????????

        private async Task<string> CreateRefreshTokenAsync(int userId)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var expiryDays  = int.Parse(jwtSettings["RefreshTokenExpiryDays"] ?? "7");

            var tokenValue   = _tokenService.GenerateRefreshToken();
            var refreshToken = new RefreshToken
            {
                UserId    = userId,
                Token     = tokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            await _repository.AddRefreshTokenAsync(refreshToken);
            await _repository.SaveChangesAsync();
            return tokenValue;
        }

        private static string GenerateSecureToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static string GenerateOtp()
        {
            var bytes = new byte[4];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            // Produces a 6-digit number in the range 100000–999999
            var value = (BitConverter.ToUInt32(bytes, 0) % 900000) + 100000;
            return value.ToString();
        }
    }
}
