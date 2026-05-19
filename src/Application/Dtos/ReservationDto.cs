namespace FODUN.Reservations.Application.Dtos;

public sealed record ReservationDto(
    Guid Id,
    Guid UserId,
    string UserFullName,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int TotalPersons,
    int NumberOfRoomsNeeded,
    string Status,
    decimal TotalCost,
    bool LaundryService,
    decimal LaundryServiceCost,
    string? Notes,
    DateTime CreatedAt,
    DateTime? CancelledAt,
    string? CancelledReason,
    IEnumerable<ReservationItemDto> Items,
    string PlaceName,
    bool HasPaymentReceipt
);

public sealed record ReservationItemDto(
    Guid SeatId,
    string SeatNumber,
    decimal PricePerNight,
    int Nights,
    decimal Subtotal
);
