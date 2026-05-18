using FODUN.Reservations.Domain.Aggregates;
using FODUN.Reservations.Domain.Exceptions;

namespace FODUN.Reservations.Domain.Aggregates.Accommodation;

public sealed class Seat : BaseEntity
{
    private readonly List<Tariff> _tariffs = new();
    private readonly List<Blackout> _blackouts = new();

    public Guid AccommodationId { get; private set; }
    public string SeatNumber { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public int Capacity { get; private set; }
    public string? Description { get; private set; }
    public string? Amenities { get; private set; }
    public bool IsActive { get; private set; } = true;
    public IReadOnlyCollection<Tariff> Tariffs => _tariffs.AsReadOnly();
    public IReadOnlyCollection<Blackout> Blackouts => _blackouts.AsReadOnly();

    private Seat() { }

    internal static Seat Create(
        Guid accommodationId,
        string seatNumber,
        string type,
        int capacity,
        string? description = null)
    {
        if (capacity <= 0)
            throw new DomainException("La capacidad del alojamiento debe ser mayor a cero.");

        return new Seat
        {
            AccommodationId = accommodationId,
            SeatNumber = seatNumber.Trim(),
            Type = type.Trim(),
            Capacity = capacity,
            Description = description?.Trim()
        };
    }

    public void SetAmenities(string amenitiesJson) { Amenities = amenitiesJson; SetUpdated(); }
    public void Deactivate() { IsActive = false; SetUpdated(); }
}
