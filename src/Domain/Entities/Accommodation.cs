namespace FODUN.Reservations.Domain.Entities;

using Enums;

public class Accommodation
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AccommodationType Type { get; set; }
    public string City { get; set; } = string.Empty;
    public string? Address { get; set; }
    public int MaxCapacity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Seat> Seats { get; set; } = [];
}
