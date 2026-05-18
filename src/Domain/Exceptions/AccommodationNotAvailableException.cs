namespace FODUN.Reservations.Domain.Exceptions;

public sealed class AccommodationNotAvailableException : DomainException
{
    public AccommodationNotAvailableException(string seatNumber)
        : base($"El alojamiento '{seatNumber}' no está disponible para las fechas seleccionadas.") { }
}
