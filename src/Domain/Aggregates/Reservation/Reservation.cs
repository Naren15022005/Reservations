using FODUN.Reservations.Domain.Aggregates;
using FODUN.Reservations.Domain.Enums;
using FODUN.Reservations.Domain.Exceptions;
using FODUN.Reservations.Domain.ValueObjects;

namespace FODUN.Reservations.Domain.Aggregates.Reservation;

public sealed class Reservation : BaseEntity
{
    private readonly List<ReservationItem> _items = new();

    public Guid UserId { get; private set; }
    public DateOnly CheckInDate { get; private set; }
    public DateOnly CheckOutDate { get; private set; }
    public int TotalPersons { get; private set; }
    public int NumberOfRoomsNeeded { get; private set; }
    public ReservationStatus Status { get; private set; } = ReservationStatus.Pending;
    public decimal TotalCost { get; private set; }
    public bool LaundryService { get; private set; }
    public decimal LaundryServiceCost { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancelledReason { get; private set; }
    public IReadOnlyCollection<ReservationItem> Items => _items.AsReadOnly();

    private Reservation() { }

    public static Reservation Create(
        Guid userId,
        DateOnly checkIn,
        DateOnly checkOut,
        int totalPersons,
        int roomsNeeded,
        decimal totalCost,
        bool laundryService = false,
        string? notes = null)
    {
        if (userId == Guid.Empty)
            throw new DomainException("El usuario es requerido.");
        if (totalPersons <= 0)
            throw new DomainException("El número de personas debe ser mayor a cero.");
        if (checkIn >= checkOut)
            throw new DomainException("El check-in debe ser anterior al check-out.");
        if (totalCost < 0)
            throw new DomainException("El costo total no puede ser negativo.");

        const decimal laundryCost = 18_000m;
        return new Reservation
        {
            UserId = userId,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            TotalPersons = totalPersons,
            NumberOfRoomsNeeded = roomsNeeded,
            TotalCost = laundryService ? totalCost + laundryCost : totalCost,
            LaundryService = laundryService,
            LaundryServiceCost = laundryService ? laundryCost : 0,
            Notes = notes?.Trim()
        };
    }

    public void AddItem(ReservationItem item)
    {
        if (Status != ReservationStatus.Pending)
            throw new DomainException("Solo se pueden agregar ítems a reservas en estado Pendiente.");
        _items.Add(item);
        SetUpdated();
    }

    public void Confirm()
    {
        if (Status != ReservationStatus.Pending)
            throw new DomainException("Solo se pueden confirmar reservas en estado Pendiente.");
        Status = ReservationStatus.Confirmed;
        SetUpdated();
    }

    public void CheckIn()
    {
        if (Status != ReservationStatus.Confirmed)
            throw new DomainException("El check-in requiere que la reserva esté confirmada.");
        Status = ReservationStatus.CheckedIn;
        SetUpdated();
    }

    public void CheckOut()
    {
        if (Status != ReservationStatus.CheckedIn)
            throw new DomainException("El check-out requiere que se haya realizado el check-in.");
        Status = ReservationStatus.CheckedOut;
        SetUpdated();
    }

    public void Cancel(string reason)
    {
        if (Status == ReservationStatus.Cancelled)
            throw new DomainException("La reserva ya está cancelada.");
        if (Status == ReservationStatus.CheckedOut)
            throw new DomainException("No se puede cancelar una reserva que ya finalizó.");
        Status = ReservationStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancelledReason = reason?.Trim();
        SetUpdated();
    }

    public bool CanBeCancelled() =>
        Status is ReservationStatus.Pending or ReservationStatus.Confirmed;

    public int Nights => CheckOutDate.DayNumber - CheckInDate.DayNumber;
}
