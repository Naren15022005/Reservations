namespace FODUN.Reservations.Domain.Entities;

public class ReservationItem
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public Guid SeatId { get; set; }
    public Guid? TariffId { get; set; }
    public decimal PricePerNight { get; set; }
    public int Nights { get; set; }
    public decimal Subtotal { get; set; }

    // Navigation
    public Reservation Reservation { get; set; } = null!;
    public Seat Seat { get; set; } = null!;
    public Tariff? Tariff { get; set; }
}
