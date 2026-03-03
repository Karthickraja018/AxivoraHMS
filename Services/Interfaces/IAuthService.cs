using Axivora.DTOs;

namespace Axivora.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterUserDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<bool> VerifyEmailAsync(string email, string verificationCode);
        Task SendPasswordResetTokenAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string resetToken, string newPassword);
    }
}
