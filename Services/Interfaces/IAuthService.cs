using Axivora.DTOs;

namespace Axivora.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterUserDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken, int callerUserId);
        Task<bool> VerifyEmailAsync(string email, string verificationCode);
        Task SendPasswordResetTokenAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string resetToken, string newPassword);
    }
}
