using FluentValidation;
using FODUN.Reservations.Application.Commands.CreateUser;

namespace FODUN.Reservations.Application.Validators;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.DocumentNumber)
            .NotEmpty().WithMessage("El número de documento es requerido.")
            .MaximumLength(20).WithMessage("El documento no puede superar 20 caracteres.")
            .Matches(@"^\d+$").WithMessage("El documento solo debe contener números.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("El nombre completo es requerido.")
            .MaximumLength(255).WithMessage("El nombre no puede superar 255 caracteres.")
            .MinimumLength(3).WithMessage("El nombre debe tener al menos 3 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El correo electrónico no tiene formato válido.")
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .Matches("[A-Z]").WithMessage("La contraseña debe contener al menos una mayúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe contener al menos un número.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("El teléfono no puede superar 20 caracteres.")
            .Matches(@"^\+?[\d\s\-\(\)]+$").WithMessage("Formato de teléfono inválido.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}
