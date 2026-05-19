namespace FODUN.Reservations.Application.Commands.CancelReservation;

public sealed record CancelReservationCommand(
    Guid ReservationId,
    Guid RequestingUserId,
    string CancellationReason
);
