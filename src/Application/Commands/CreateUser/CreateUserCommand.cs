namespace FODUN.Reservations.Application.Commands.CreateUser;

public sealed record CreateUserCommand(
    string DocumentNumber,
    string FullName,
    string Email,
    string Password,
    string? PhoneNumber
);
