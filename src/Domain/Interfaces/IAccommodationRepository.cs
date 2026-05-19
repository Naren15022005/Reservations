using FODUN.Reservations.Domain.Aggregates.Accommodation;

namespace FODUN.Reservations.Domain.Interfaces;

public interface IAccommodationRepository
{
    Task<Accommodation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Accommodation?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<Accommodation>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<Seat?> GetSeatByIdAsync(Guid seatId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Seat>> GetAvailableSeatsAsync(Guid accommodationId, DateOnly checkIn, DateOnly checkOut, int minCapacity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fechas ocupadas (yyyy-MM-dd) en el rango dado para uso en el calendario de disponibilidad.
    /// </summary>
    Task<IReadOnlySet<string>> GetOccupiedDatesAsync(
        IEnumerable<Guid> seatIds, DateOnly from, DateOnly until,
        CancellationToken cancellationToken = default);
}
