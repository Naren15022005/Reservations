using FODUN.Reservations.Domain.Aggregates.Reservation;
using FODUN.Reservations.Domain.Enums;
using FODUN.Reservations.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FODUN.Reservations.Infrastructure.Persistence.Repositories;

public sealed class ReservationRepository : IReservationRepository
{
    private readonly ReservationsDbContext _context;

    public ReservationRepository(ReservationsDbContext context) => _context = context;

    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Reservations
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IEnumerable<Reservation>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _context.Reservations
            .Include(r => r.Items)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> HasConflictingReservationAsync(
        Guid seatId,
        DateOnly checkIn,
        DateOnly checkOut,
        Guid? excludeReservationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReservationItems
            .Join(_context.Reservations,
                ri => ri.ReservationId,
                r => r.Id,
                (ri, r) => new { ri.SeatId, ri.ReservationId, r.Status, r.CheckInDate, r.CheckOutDate })
            .Where(x => x.SeatId == seatId &&
                        (x.Status == ReservationStatus.Pending || x.Status == ReservationStatus.Confirmed || x.Status == ReservationStatus.CheckedIn) &&
                        x.CheckInDate < checkOut &&
                        x.CheckOutDate > checkIn);

        if (excludeReservationId.HasValue)
            query = query.Where(x => x.ReservationId != excludeReservationId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default) =>
        await _context.Reservations.AddAsync(reservation, cancellationToken);

    public Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        _context.Reservations.Update(reservation);
        return Task.CompletedTask;
    }
}
