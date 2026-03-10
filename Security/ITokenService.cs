using System;
using System.Security.Claims;

namespace Axivora.Security
{
    public interface ITokenService
    {
        string GenerateJwtToken(int userId, string email, string role);
        DateTime GetJwtExpiryTime();
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
