namespace FODUN.Reservations.Application.Queries.GetAvailableRooms;

public sealed record GetAvailableRoomsQuery(
    Guid AccommodationId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int TotalPersons
);
