namespace FODUN.Reservations.Application.Commands.CreateReservation;

public sealed record CreateReservationCommand(
    Guid UserId,
    Guid AccommodationId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int TotalPersons,
    bool IncludeLaundry,
    string? Notes,
    Guid? SeatId = null
);
