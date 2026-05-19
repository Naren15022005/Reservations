namespace FODUN.Reservations.Application.Dtos;

public sealed record TariffDto(
    Guid Id,
    Guid SeatId,
    string Season,
    int MinPersons,
    int MaxPersons,
    decimal PricePerNight,
    decimal? AdditionalPersonPrice,
    bool IsExceptional,
    DateOnly ValidFrom,
    DateOnly? ValidUntil
);
