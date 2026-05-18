using FODUN.Reservations.Domain.Aggregates;
using FODUN.Reservations.Domain.Exceptions;
using FODUN.Reservations.Domain.ValueObjects;

namespace FODUN.Reservations.Domain.Aggregates.Accommodation;

public sealed class Blackout : BaseEntity
{
    public Guid SeatId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string? Reason { get; private set; }

    private Blackout() { }

    public static Blackout Create(Guid seatId, DateOnly startDate, DateOnly endDate, string? reason = null)
    {
        if (startDate >= endDate)
            throw new DomainException("La fecha de inicio del bloqueo debe ser anterior a la fecha de fin.");
        return new Blackout { SeatId = seatId, StartDate = startDate, EndDate = endDate, Reason = reason };
    }

    public bool ConflictsWith(DateOnly checkIn, DateOnly checkOut) =>
        StartDate < checkOut && EndDate > checkIn;
}
