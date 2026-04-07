using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Repositories.Interfaces;
using Axivora.Security;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAuthRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;

        private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);

        public AuthenticationService(
            IAuthRepository repository,
            IConfiguration configuration,
            ITokenService tokenService,
            IPasswordHasher passwordHasher,
            IEmailService emailService)
        {
            _repository = repository;
            _configuration = configuration;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto registerDto)
        {
            if (await _repository.EmailExistsAsync(registerDto.Email))
                throw new InvalidOperationException("Email already registered.");

            if (registerDto.Role != "Patient")
                throw new InvalidOperationException("Self-registration is only allowed for Patient role. Admins and Doctors must be created by system administrators.");

            var user = new User
            {
                Email = registerDto.Email,
                PasswordHash = _passwordHasher.Hash(registerDto.Password),
                IsActive = true,
                IsDeleted = false,
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.AddUserAsync(user);
            await _repository.SaveChangesAsync();

            var role = await _repository.GetRoleByNameAsync(registerDto.Role);
            if (role == null)
            {
                role = new Role { RoleName = registerDto.Role };
                await _repository.AddRoleAsync(role);
                await _repository.SaveChangesAsync();
            }

            await _repository.AddUserRoleAsync(new UserRole { UserId = user.UserId, RoleId = role.RoleId });
            await _repository.SaveChangesAsync();

            var token = _tokenService.GenerateJwtToken(user.UserId, user.Email, registerDto.Role);
            var refreshToken = await CreateRefreshTokenAsync(user.UserId);

            var profileCompleted = registerDto.Role == "Patient"
                ? await _repository.PatientProfileExistsAsync(user.UserId)
                : registerDto.Role == "Doctor" && await _repository.DoctorProfileExistsAsync(user.UserId);

            return new AuthResponseDto
            {
                UserId = user.UserId,
                Email = user.Email,
                Token = token,
                RefreshToken = refreshToken,
                TokenExpiresAt = _tokenService.GetJwtExpiryTime(),
                Role = registerDto.Role,
                EmailVerified = false,
                ProfileCompleted = profileCompleted,
                MustChangePassword = user.MustChangePassword
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _repository.GetUserByEmailAsync(loginDto.Email);
            if (user == null || user.IsDeleted)
                throw new UnauthorizedAccessException("Invalid email or password.");

            if (!_passwordHasher.Verify(loginDto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password.");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is disabled. Please contact support.");

            var roleName = user.UserRoles.FirstOrDefault()?.Role?.RoleName ?? "Patient";
            var token = _tokenService.GenerateJwtToken(user.UserId, user.Email, roleName);
            var refreshToken = await CreateRefreshTokenAsync(user.UserId);

            var profileCompleted = roleName == "Patient"
                ? await _repository.PatientProfileExistsAsync(user.UserId)
                : roleName == "Doctor" && await _repository.DoctorProfileExistsAsync(user.UserId);

            user.UpdatedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync();

            return new AuthResponseDto
            {
                UserId = user.UserId,
                Email = user.Email,
                Token = token,
                RefreshToken = refreshToken,
                TokenExpiresAt = _tokenService.GetJwtExpiryTime(),
                Role = roleName,
                EmailVerified = user.IsEmailVerified,
                ProfileCompleted = profileCompleted,
                MustChangePassword = user.MustChangePassword
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

            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;

            var roleName = user.UserRoles.FirstOrDefault()?.Role?.RoleName ?? "Patient";
            var newJwtToken = _tokenService.GenerateJwtToken(user.UserId, user.Email, roleName);
            var newRefreshToken = await CreateRefreshTokenAsync(user.UserId);

            var profileCompleted = roleName == "Patient"
                ? await _repository.PatientProfileExistsAsync(user.UserId)
                : roleName == "Doctor" && await _repository.DoctorProfileExistsAsync(user.UserId);

            await _repository.SaveChangesAsync();

            return new AuthResponseDto
            {
                UserId = user.UserId,
                Email = user.Email,
                Token = newJwtToken,
                RefreshToken = newRefreshToken,
                TokenExpiresAt = _tokenService.GetJwtExpiryTime(),
                Role = roleName,
                EmailVerified = user.IsEmailVerified,
                ProfileCompleted = profileCompleted,
                MustChangePassword = user.MustChangePassword
            };
        }

        public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _repository.GetUserByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            if (!_passwordHasher.Verify(currentPassword, user.PasswordHash))
                throw new UnauthorizedAccessException("Current password is incorrect.");

            user.PasswordHash = _passwordHasher.Hash(newPassword);
            user.MustChangePassword = false;
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenExpiresAt = null;
            user.PasswordResetRequestedAt = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync();
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

            user.IsEmailVerified = true;
            user.EmailVerificationOtp = null;
            user.OtpExpiresAt = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync();
        }

        public async Task ResendEmailVerificationOtpAsync(string email)
        {
            var user = await _repository.GetUserByEmailAsync(email)
                ?? throw new KeyNotFoundException("User not found.");

            if (user.IsEmailVerified)
                throw new InvalidOperationException("Email is already verified.");

            if (user.OtpExpiresAt.HasValue)
            {
                var issuedAt = user.OtpExpiresAt.Value - OtpLifetime;
                var cooldownEndsAt = issuedAt + ResendCooldown;
                if (DateTime.UtcNow < cooldownEndsAt)
                {
                    var wait = (cooldownEndsAt - DateTime.UtcNow).Seconds;
                    throw new InvalidOperationException($"Please wait {wait} second(s) before requesting a new OTP.");
                }
            }

            var otp = GenerateOtp();
            var expiresAt = DateTime.UtcNow.Add(OtpLifetime);
            await _repository.SaveOtpAsync(user.UserId, _passwordHasher.Hash(otp), expiresAt);
            await _repository.SaveChangesAsync();
            await _emailService.SendEmailVerificationOtpAsync(email, otp);
        }

        public async Task<bool> IsEmailAvailableAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(email));

            return !await _repository.EmailExistsAsync(email);
        }

        private async Task<string> CreateRefreshTokenAsync(int userId)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var expiryDays = int.Parse(jwtSettings["RefreshTokenExpiryDays"] ?? "7");

            var tokenValue = _tokenService.GenerateRefreshToken();
            var refreshToken = new RefreshToken
            {
                UserId = userId,
                Token = tokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            await _repository.AddRefreshTokenAsync(refreshToken);
            await _repository.SaveChangesAsync();
            return tokenValue;
        }

        private static string GenerateOtp()
        {
            var bytes = new byte[4];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            var value = (BitConverter.ToUInt32(bytes, 0) % 900000) + 100000;
            return value.ToString();
        }
    }
}
