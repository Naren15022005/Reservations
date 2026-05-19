namespace FODUN.Reservations.Application.Dtos;

public sealed record AccommodationDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Type,
    string City,
    string? Address,
    int MaxCapacity,
    IEnumerable<SeatDto> Seats
);

public sealed record SeatDto(
    Guid Id,
    string SeatNumber,
    string Type,
    int Capacity,
    string? Description,
    string? Amenities
);
