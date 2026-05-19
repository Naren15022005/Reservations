using FODUN.Reservations.Application.Commands.CreateUser;
using FODUN.Reservations.Application.Common;
using FODUN.Reservations.Application.Dtos;

namespace FODUN.Reservations.Application.Services;

public interface IUserService
{
    Task<Result<UserDto>> RegisterAsync(CreateUserCommand command, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> GetOrCreateExternalUserAsync(string email, string name, string provider, string externalId, CancellationToken cancellationToken = default);
    Task<Result> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> UpdateProfileAsync(Guid userId, string fullName, string? phoneNumber, CancellationToken cancellationToken = default);
}
