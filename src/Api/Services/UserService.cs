using FODUN.Reservations.Application.Commands.CreateUser;
using FODUN.Reservations.Application.Common;
using FODUN.Reservations.Application.Dtos;
using FODUN.Reservations.Application.Services;
using FODUN.Reservations.Domain.Aggregates.User;
using FODUN.Reservations.Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace FODUN.Reservations.Api.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IValidator<CreateUserCommand> _validator;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IConfiguration configuration,
        IValidator<CreateUserCommand> validator,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _configuration = configuration;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<UserDto>> RegisterAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<UserDto>.Failure(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        if (await _userRepository.ExistsByEmailAsync(command.Email, cancellationToken))
            return Result<UserDto>.Failure("Ya existe una cuenta con ese correo electrónico.");

        if (await _userRepository.ExistsByDocumentNumberAsync(command.DocumentNumber, cancellationToken))
            return Result<UserDto>.Failure("Ya existe una cuenta con ese número de documento.");

        var (hash, salt) = _passwordHasher.Hash(command.Password);
        var user = User.Create(command.DocumentNumber, command.FullName, command.Email, hash, salt, command.PhoneNumber);

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var baseUrl = _configuration["AppSettings:BaseUrl"];
        var confirmationLink = $"{baseUrl}/Auth/ConfirmEmail?token={user.EmailConfirmationToken}&userId={user.Id}";

        try
        {
            await _emailService.SendEmailConfirmationAsync(user.Email, user.FullName, confirmationLink, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo enviar el correo de confirmación a {Email}", user.Email);
        }

        _logger.LogInformation("Usuario registrado: {Email}", user.Email);
        return Result<UserDto>.Success(MapToDto(user));
    }

    public async Task<Result<UserDto>> GetOrCreateExternalUserAsync(
        string email, string name, string provider, string externalId, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            if (existing.ExternalId is null)
                existing.LinkExternalProvider(provider, externalId);
            existing.RegisterSuccessfulLogin();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Login externo ({Provider}): {Email}", provider, email);
            return Result<UserDto>.Success(MapToDto(existing));
        }

        var user = User.CreateFromExternal(name, email, provider, externalId);
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Usuario creado vía {Provider}: {Email}", provider, email);
        return Result<UserDto>.Success(MapToDto(user));
    }

    public async Task<Result<UserDto>> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
            return Result<UserDto>.Failure("Credenciales inválidas.");

        if (user.IsLockedOut())
            return Result<UserDto>.Failure("La cuenta está bloqueada temporalmente. Intente de nuevo en unos minutos.");

        if (!_passwordHasher.Verify(password, user.PasswordHash, user.PasswordSalt))
        {
            user.RegisterFailedLogin();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<UserDto>.Failure("Credenciales inválidas.");
        }

        user.RegisterSuccessfulLogin();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Login exitoso: {Email}", user.Email);
        return Result<UserDto>.Success(MapToDto(user));
    }

    public async Task<Result> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        // No revelamos si el correo existe o no (seguridad)
        if (user is null)
            return Result.Success();

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        user.SetPasswordResetToken(token, expiryMinutes: 30);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var baseUrl = _configuration["AppSettings:BaseUrl"];
        var resetLink = $"{baseUrl}/Auth/ResetPassword?token={Uri.EscapeDataString(token)}";
        await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetLink, cancellationToken);

        _logger.LogInformation("Solicitud de reset de contraseña para: {Email}", email);
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByPasswordResetTokenAsync(token, cancellationToken);
        if (user is null || !user.IsPasswordResetTokenValid(token))
            return Result.Failure("El enlace de recuperación es inválido o ha expirado.");

        var (hash, salt) = _passwordHasher.Hash(newPassword);
        user.ResetPassword(hash, salt);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Contraseña restablecida para usuario: {Id}", user.Id);
        return Result.Success();
    }

    public async Task<Result> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || user.EmailConfirmationToken != token)
            return Result.Failure("El enlace de confirmación no es válido.");

        if (user.IsEmailConfirmed)
            return Result.Success();

        user.ConfirmEmail();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Email confirmado para usuario: {Id}", userId);
        return Result.Success();
    }

    public async Task<Result<UserDto>> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null
            ? Result<UserDto>.Failure("Usuario no encontrado.")
            : Result<UserDto>.Success(MapToDto(user));
    }

    public async Task<Result<UserDto>> UpdateProfileAsync(Guid userId, string fullName, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) return Result<UserDto>.Failure("Usuario no encontrado.");

        if (string.IsNullOrWhiteSpace(fullName))
            return Result<UserDto>.Failure("El nombre completo es obligatorio.");

        user.UpdateProfile(fullName, phoneNumber);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Perfil actualizado para usuario: {Id}", userId);
        return Result<UserDto>.Success(MapToDto(user));
    }

    private static UserDto MapToDto(User user) => new(
        user.Id, user.DocumentNumber, user.FullName, user.Email,
        user.PhoneNumber, user.IsEmailConfirmed, user.IsActive, user.CreatedAt);
}
