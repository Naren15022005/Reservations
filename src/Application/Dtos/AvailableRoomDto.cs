namespace FODUN.Reservations.Application.Dtos;

public sealed record AvailableRoomDto(
    Guid SeatId,
    string SeatNumber,
    string Type,
    int Capacity,
    string? Description,
    string? Amenities,
    decimal PricePerNight,
    string Season
);
