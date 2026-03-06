using System;

namespace Axivora.Security
{
    public interface ITokenService
    {
        string GenerateJwtToken(int userId, string email, string role);
    }
}
