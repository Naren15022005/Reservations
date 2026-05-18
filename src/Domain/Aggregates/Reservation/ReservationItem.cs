using FODUN.Reservations.Domain.Aggregates;
using FODUN.Reservations.Domain.Exceptions;

namespace FODUN.Reservations.Domain.Aggregates.Reservation;

public sealed class ReservationItem : BaseEntity
{
    public Guid ReservationId { get; private set; }
    public Guid SeatId { get; private set; }
    public Guid? TariffId { get; private set; }
    public decimal PricePerNight { get; private set; }
    public int Nights { get; private set; }
    public decimal Subtotal => PricePerNight * Nights;

    private ReservationItem() { }

    public static ReservationItem Create(
        Guid reservationId,
        Guid seatId,
        decimal pricePerNight,
        int nights,
        Guid? tariffId = null)
    {
        if (pricePerNight < 0)
            throw new DomainException("El precio por noche no puede ser negativo.");
        if (nights <= 0)
            throw new DomainException("El número de noches debe ser mayor a cero.");

        return new ReservationItem
        {
            ReservationId = reservationId,
            SeatId = seatId,
            TariffId = tariffId,
            PricePerNight = pricePerNight,
            Nights = nights
        };
    }
}
