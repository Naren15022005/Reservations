namespace FODUN.Reservations.Domain.Entities;

using Enums;

public class Tariff
{
    public Guid Id { get; set; }
    public Guid SeatId { get; set; }
    public Season Season { get; set; }
    public int MinPersons { get; set; }
    public int MaxPersons { get; set; }
    public decimal PricePerNight { get; set; }
    public decimal AdditionalPersonPrice { get; set; }
    public string? SpecialDays { get; set; } // JSON or string (MonTueWedThu)
    public bool IsSpecial { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Seat Seat { get; set; } = null!;
    public ICollection<ReservationItem> ReservationItems { get; set; } = [];
}
