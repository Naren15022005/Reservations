using FODUN.Reservations.Domain.Constants;
using FODUN.Reservations.Domain.Aggregates;
using FODUN.Reservations.Domain.Exceptions;

namespace FODUN.Reservations.Domain.Aggregates.User;

public sealed class User : BaseEntity
{
    public string DocumentNumber { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public string? PasswordSalt { get; private set; }
    public bool IsEmailConfirmed { get; private set; }
    public string? EmailConfirmationToken { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiry { get; private set; }
    public bool LockoutEnabled { get; private set; }
    public DateTime? LockoutEndTime { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }
    public string? ExternalProvider { get; private set; }
    public string? ExternalId { get; private set; }

    private User() { }

    public static User Create(
        string documentNumber,
        string fullName,
        string email,
        string passwordHash,
        string? passwordSalt = null,
        string? phoneNumber = null)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
            throw new DomainException("El número de documento es requerido.");
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("El nombre completo es requerido.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("El correo electrónico es requerido.");

        return new User
        {
            DocumentNumber = documentNumber.Trim(),
            FullName = fullName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            PhoneNumber = phoneNumber?.Trim(),
            EmailConfirmationToken = Guid.NewGuid().ToString("N")
        };
    }

    public static User CreateFromExternal(
        string fullName,
        string email,
        string provider,
        string externalId)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("El correo electrónico es requerido.");

        var docNumber = $"EXT-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        return new User
        {
            DocumentNumber = docNumber,
            FullName = string.IsNullOrWhiteSpace(fullName) ? email.Split('@')[0] : fullName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = string.Empty,
            IsEmailConfirmed = true,
            ExternalProvider = provider,
            ExternalId = externalId
        };
    }

    public void LinkExternalProvider(string provider, string externalId)
    {
        ExternalProvider = provider;
        ExternalId = externalId;
        IsEmailConfirmed = true;
        SetUpdated();
    }

    public void ConfirmEmail()
    {
        IsEmailConfirmed = true;
        EmailConfirmationToken = null;
        SetUpdated();
    }

    public void SetPasswordResetToken(string token, int expiryMinutes = 30)
    {
        PasswordResetToken = token;
        PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(expiryMinutes);
        SetUpdated();
    }

    public void ResetPassword(string newPasswordHash, string? newSalt)
    {
        PasswordHash = newPasswordHash;
        PasswordSalt = newSalt;
        PasswordResetToken = null;
        PasswordResetTokenExpiry = null;
        FailedLoginAttempts = 0;
        SetUpdated();
    }

    public void RegisterFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= BusinessRules.MaxFailedLoginAttempts)
        {
            LockoutEnabled = true;
            LockoutEndTime = DateTime.UtcNow.AddMinutes(BusinessRules.LockoutDurationMinutes);
        }
        SetUpdated();
    }

    public void RegisterSuccessfulLogin()
    {
        FailedLoginAttempts = 0;
        LockoutEnabled = false;
        LockoutEndTime = null;
        SetUpdated();
    }

    public bool IsLockedOut() =>
        LockoutEnabled && LockoutEndTime.HasValue && LockoutEndTime > DateTime.UtcNow;

    public bool IsPasswordResetTokenValid(string token) =>
        PasswordResetToken == token &&
        PasswordResetTokenExpiry.HasValue &&
        PasswordResetTokenExpiry > DateTime.UtcNow;

    public void UpdateProfile(string fullName, string? phoneNumber)
    {
        FullName    = fullName.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        SetUpdated();
    }

    public void Deactivate() { IsActive = false; SetUpdated(); }
}
