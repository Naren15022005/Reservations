namespace FODUN.Reservations.Domain.Entities;

public class Blackout
{
    public Guid Id { get; set; }
    public Guid SeatId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Seat Seat { get; set; } = null!;
}
