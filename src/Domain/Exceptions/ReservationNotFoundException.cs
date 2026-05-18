namespace FODUN.Reservations.Domain.Exceptions;

public sealed class ReservationNotFoundException : DomainException
{
    public ReservationNotFoundException(Guid id)
        : base($"No se encontró la reserva con Id '{id}'.") { }
}
