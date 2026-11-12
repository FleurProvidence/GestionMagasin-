using System.Security.Cryptography;
using System.Text;

namespace GestionMagasin.Services.Security;

public sealed class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltLength = 16;
    private const int HashLength = 32;

    public (string Hash, string Sel, string Parametres) HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Le mot de passe est requis.", nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashLength);

        return (
            Convert.ToBase64String(hash),
            Convert.ToBase64String(salt),
            "pbkdf2-sha256;iterations=100000;salt=16;hash=32"
        );
    }

    public bool VerifyPassword(string password, string storedHash, string storedSalt, string storedParameters)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        try
        {
            if (!storedParameters.Contains("pbkdf2-sha256", StringComparison.OrdinalIgnoreCase))
                return false;

            var salt = Convert.FromBase64String(storedSalt);
            var hash = Convert.FromBase64String(storedHash);

            var computed = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashLength);

            return CryptographicOperations.FixedTimeEquals(computed, hash);
        }
        catch
        {
            return false;
        }
    }
}
