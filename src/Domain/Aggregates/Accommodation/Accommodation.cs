using FODUN.Reservations.Domain.Aggregates;
using FODUN.Reservations.Domain.Enums;
using FODUN.Reservations.Domain.Exceptions;

namespace FODUN.Reservations.Domain.Aggregates.Accommodation;

public sealed class Accommodation : BaseEntity
{
    private readonly List<Seat> _seats = new();

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public AccommodationType Type { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public int MaxCapacity { get; private set; }
    public bool IsActive { get; private set; } = true;
    public IReadOnlyCollection<Seat> Seats => _seats.AsReadOnly();

    private Accommodation() { }

    public static Accommodation Create(
        string code,
        string name,
        AccommodationType type,
        string city,
        int maxCapacity,
        string? description = null,
        string? address = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("El código de la sede es requerido.");
        if (maxCapacity <= 0)
            throw new DomainException("La capacidad máxima debe ser mayor a cero.");

        return new Accommodation
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Description = description?.Trim(),
            Type = type,
            City = city.Trim(),
            Address = address?.Trim(),
            MaxCapacity = maxCapacity
        };
    }

    public Seat AddSeat(string seatNumber, string seatType, int capacity, string? description = null)
    {
        if (_seats.Any(s => s.SeatNumber.Equals(seatNumber, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Ya existe un alojamiento con el número '{seatNumber}'.");

        var seat = Seat.Create(Id, seatNumber, seatType, capacity, description);
        _seats.Add(seat);
        SetUpdated();
        return seat;
    }

    public void Deactivate() { IsActive = false; SetUpdated(); }
}
