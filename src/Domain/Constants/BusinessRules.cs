namespace FODUN.Reservations.Domain.Constants;

public static class BusinessRules
{
    public const decimal LaundryFee                          = 18_000m;
    public const decimal AdditionalPersonFee                 = 16_000m;

    public const int MaxFailedLoginAttempts                  = 5;
    public const int LockoutDurationMinutes                  = 15;
    public const int PasswordResetTokenExpirationMinutes     = 30;
    public const int EmailConfirmationTokenExpirationHours   = 24;

    public const int MaxNotesLength                          = 1000;
    public const int MaxEmailLength                          = 255;
    public const int MaxDocumentNumberLength                 = 20;
    public const int MaxNameLength                           = 255;
    public const int MinPasswordLength                       = 8;
}
