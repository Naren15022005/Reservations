namespace FODUN.Reservations.Application.Dtos;

public sealed record UserDto(
    Guid Id,
    string DocumentNumber,
    string FullName,
    string Email,
    string? PhoneNumber,
    bool IsEmailConfirmed,
    bool IsActive,
    DateTime CreatedAt
);
