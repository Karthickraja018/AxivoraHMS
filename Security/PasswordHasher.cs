namespace Axivora.Security
{
    public class PasswordHasher : IPasswordHasher
    {
        public string Hash(string password) =>
            BCrypt.Net.BCrypt.HashPassword(password);

        public bool Verify(string password, string passwordHash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return false;
            }
        }
    }
}
