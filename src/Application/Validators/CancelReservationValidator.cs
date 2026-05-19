using FluentValidation;
using FODUN.Reservations.Application.Commands.CancelReservation;

namespace FODUN.Reservations.Application.Validators;

public sealed class CancelReservationValidator : AbstractValidator<CancelReservationCommand>
{
    public CancelReservationValidator()
    {
        RuleFor(x => x.ReservationId)
            .NotEmpty().WithMessage("El Id de la reserva es requerido.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty().WithMessage("El usuario solicitante es requerido.");

        RuleFor(x => x.CancellationReason)
            .NotEmpty().WithMessage("El motivo de cancelación es requerido.")
            .MaximumLength(500).WithMessage("El motivo no puede superar 500 caracteres.");
    }
}
