namespace FODUN.Reservations.Domain.Entities;

using Enums;

public class Reservation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int TotalPersons { get; set; }
    public int NumberOfRoomsNeeded { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public decimal TotalCost { get; set; }
    public bool LaundryService { get; set; }
    public decimal LaundryServiceCost { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledReason { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<ReservationItem> Items { get; set; } = [];
}
