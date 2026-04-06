using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Axivora.Repositories.Interfaces;
using Axivora.Security;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    public class PasswordRecoveryService : IPasswordRecoveryService
    {
        private static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromMinutes(15);

        private readonly IAuthRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;

        public PasswordRecoveryService(
            IAuthRepository repository,
            IConfiguration configuration,
            IPasswordHasher passwordHasher,
            IEmailService emailService)
        {
            _repository = repository;
            _configuration = configuration;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
        }

        public async Task SendPasswordResetTokenAsync(string email)
        {
            var user = await _repository.GetUserByEmailAsync(email);

            // Prevent account enumeration: return success semantics even when user does not exist.
            if (user == null || !user.IsActive || user.IsDeleted)
            {
                _ = GenerateUrlSafeToken();
                return;
            }

            var resetToken = GenerateUrlSafeToken();
            var resetTokenHash = _passwordHasher.Hash(resetToken);
            var expiresAt = DateTime.UtcNow.Add(PasswordResetLifetime);

            user.PasswordResetTokenHash = resetTokenHash;
            user.PasswordResetTokenExpiresAt = expiresAt;
            user.PasswordResetRequestedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _repository.SaveChangesAsync();

            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://axivora.health";
            var resetLink = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(email)}";
            await _emailService.SendForgotPasswordEmailAsync(email, resetLink);
        }

        public async Task<bool> ResetPasswordAsync(string email, string resetToken, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(resetToken) || string.IsNullOrWhiteSpace(email))
                return false;

            var user = await _repository.GetUserByEmailAsync(email);
            if (user == null || !user.IsActive || user.IsDeleted)
                return false;

            if (string.IsNullOrWhiteSpace(user.PasswordResetTokenHash) || !user.PasswordResetTokenExpiresAt.HasValue)
                return false;

            if (user.PasswordResetTokenExpiresAt.Value < DateTime.UtcNow)
                return false;

            if (!_passwordHasher.Verify(resetToken, user.PasswordResetTokenHash))
                return false;

            user.PasswordHash = _passwordHasher.Hash(newPassword);
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenExpiresAt = null;
            user.PasswordResetRequestedAt = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _repository.SaveChangesAsync();
            return true;
        }

        private static string GenerateUrlSafeToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
