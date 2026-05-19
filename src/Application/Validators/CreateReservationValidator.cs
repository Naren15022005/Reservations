using FluentValidation;
using FODUN.Reservations.Application.Commands.CreateReservation;

namespace FODUN.Reservations.Application.Validators;

public sealed class CreateReservationValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El usuario es requerido.");

        RuleFor(x => x.AccommodationId)
            .NotEmpty().WithMessage("La sede o apartamento es requerida.");

        RuleFor(x => x.CheckIn)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .WithMessage("La fecha de check-in no puede ser en el pasado.");

        RuleFor(x => x.CheckOut)
            .GreaterThan(x => x.CheckIn)
            .WithMessage("La fecha de check-out debe ser posterior al check-in.");

        RuleFor(x => x.TotalPersons)
            .GreaterThan(0).WithMessage("El número de personas debe ser mayor a cero.")
            .LessThanOrEqualTo(100).WithMessage("El número de personas no puede superar 100.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Las notas no pueden superar 1000 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
