using Axivora.DTOs;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    /// <summary>
    /// Backward-compatible facade over focused auth sub-services.
    /// Controllers can keep depending on IAuthService while responsibilities stay separated.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IPasswordRecoveryService _passwordRecoveryService;

        public AuthService(
            IAuthenticationService authenticationService,
            IPasswordRecoveryService passwordRecoveryService)
        {
            _authenticationService = authenticationService;
            _passwordRecoveryService = passwordRecoveryService;
        }

        public Task<AuthResponseDto> RegisterAsync(RegisterUserDto registerDto) =>
            _authenticationService.RegisterAsync(registerDto);

        public Task<AuthResponseDto> LoginAsync(LoginDto loginDto) =>
            _authenticationService.LoginAsync(loginDto);

        public Task<AuthResponseDto> RefreshTokenAsync(string refreshToken) =>
            _authenticationService.RefreshTokenAsync(refreshToken);

        public Task<bool> RevokeTokenAsync(string refreshToken, int callerUserId) =>
            _authenticationService.RevokeTokenAsync(refreshToken, callerUserId);

        public Task VerifyEmailOtpAsync(string email, string otp) =>
            _authenticationService.VerifyEmailOtpAsync(email, otp);

        public Task ResendEmailVerificationOtpAsync(string email) =>
            _authenticationService.ResendEmailVerificationOtpAsync(email);

        public Task SendPasswordResetTokenAsync(string email) =>
            _passwordRecoveryService.SendPasswordResetTokenAsync(email);

        public Task<bool> ResetPasswordAsync(string email, string resetToken, string newPassword) =>
            _passwordRecoveryService.ResetPasswordAsync(email, resetToken, newPassword);

        public Task ChangePasswordAsync(int userId, string currentPassword, string newPassword) =>
            _authenticationService.ChangePasswordAsync(userId, currentPassword, newPassword);

        public Task<bool> IsEmailAvailableAsync(string email) =>
            _authenticationService.IsEmailAvailableAsync(email);
    }
}
