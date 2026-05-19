using System.Security.Cryptography;
using System.Text;
using FODUN.Reservations.Application.Services;

namespace FODUN.Reservations.Infrastructure.Services;

public sealed class PasswordHasherService : IPasswordHasher
{
    private const int SaltSize = 32;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public (string hash, string salt) Hash(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        var salt = Convert.ToBase64String(saltBytes);
        var hash = ComputeHash(password, saltBytes);
        return (hash, salt);
    }

    public bool Verify(string password, string hash, string? salt)
    {
        if (string.IsNullOrEmpty(salt)) return false;
        var saltBytes = Convert.FromBase64String(salt);
        var computedHash = ComputeHash(password, saltBytes);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(computedHash),
            Convert.FromBase64String(hash));
    }

    private static string ComputeHash(string password, byte[] salt)
    {
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            Algorithm,
            HashSize);
        return Convert.ToBase64String(hashBytes);
    }
}
