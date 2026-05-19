namespace FODUN.Reservations.Application.Dtos;

public sealed record ReservationCostDto(
    decimal BaseTotal,
    decimal AdditionalPersonsTotal,
    decimal LaundryTotal,
    decimal GrandTotal,
    int Nights,
    int AdditionalPersons
);
