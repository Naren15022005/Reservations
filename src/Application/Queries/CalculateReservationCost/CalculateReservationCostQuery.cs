namespace FODUN.Reservations.Application.Queries.CalculateReservationCost;

public sealed record CalculateReservationCostQuery(
    Guid SeatId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int TotalPersons,
    bool IncludeLaundry
);
