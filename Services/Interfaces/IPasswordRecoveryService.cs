namespace Axivora.Services.Interfaces
{
    public interface IPasswordRecoveryService
    {
        Task SendPasswordResetTokenAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string resetToken, string newPassword);
    }
}
