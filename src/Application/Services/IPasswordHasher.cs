namespace FODUN.Reservations.Application.Services;

public interface IPasswordHasher
{
    (string hash, string salt) Hash(string password);
    bool Verify(string password, string hash, string? salt);
}
