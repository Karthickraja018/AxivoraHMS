using Axivora.DTOs;

namespace Axivora.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterUserDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken, int callerUserId);
        Task VerifyEmailOtpAsync(string email, string otp);
        Task ResendEmailVerificationOtpAsync(string email);
        Task<bool> IsEmailAvailableAsync(string email);
    }
}
