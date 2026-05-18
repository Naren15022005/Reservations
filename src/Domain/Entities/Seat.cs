namespace FODUN.Reservations.Domain.Entities;

public class Seat
{
    public Guid Id { get; set; }
    public Guid AccommodationId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? Description { get; set; }
    public string? Amenities { get; set; } // JSON
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Accommodation Accommodation { get; set; } = null!;
    public ICollection<Tariff> Tariffs { get; set; } = [];
    public ICollection<Blackout> Blackouts { get; set; } = [];
    public ICollection<ReservationItem> ReservationItems { get; set; } = [];
}
