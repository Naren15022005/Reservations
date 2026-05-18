using FODUN.Reservations.Domain.Aggregates.Reservation;
using FODUN.Reservations.Domain.Enums;

namespace FODUN.Reservations.Domain.Interfaces;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reservation>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasConflictingReservationAsync(Guid seatId, DateOnly checkIn, DateOnly checkOut, Guid? excludeReservationId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default);
}
