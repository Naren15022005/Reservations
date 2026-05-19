using FODUN.Reservations.Domain.Aggregates.Accommodation;
using FODUN.Reservations.Domain.Enums;
using FODUN.Reservations.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Frozen;

namespace FODUN.Reservations.Infrastructure.Persistence.Repositories;

public sealed class AccommodationRepository : IAccommodationRepository
{
    private readonly ReservationsDbContext _context;

    public AccommodationRepository(ReservationsDbContext context) => _context = context;

    public async Task<Accommodation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Accommodations
            .Include(a => a.Seats).ThenInclude(s => s.Tariffs)
            .FirstOrDefaultAsync(a => a.Id == id && a.IsActive, cancellationToken);

    public async Task<Accommodation?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        await _context.Accommodations
            .Include(a => a.Seats).ThenInclude(s => s.Tariffs)
            .FirstOrDefaultAsync(a => a.Code == code.ToUpperInvariant() && a.IsActive, cancellationToken);

    public async Task<IEnumerable<Accommodation>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        await _context.Accommodations
            .Include(a => a.Seats)
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

    public async Task<Seat?> GetSeatByIdAsync(Guid seatId, CancellationToken cancellationToken = default) =>
        await _context.Seats
            .Include(s => s.Tariffs)
            .Include(s => s.Blackouts)
            .FirstOrDefaultAsync(s => s.Id == seatId && s.IsActive, cancellationToken);

    public async Task<IEnumerable<Seat>> GetAvailableSeatsAsync(
        Guid accommodationId,
        DateOnly checkIn,
        DateOnly checkOut,
        int minCapacity,
        CancellationToken cancellationToken = default)
    {
        var reservedSeatIds = await _context.ReservationItems
            .Join(_context.Reservations,
                ri => ri.ReservationId,
                r => r.Id,
                (ri, r) => new { ri.SeatId, r.Status, r.CheckInDate, r.CheckOutDate })
            .Where(x => (x.Status == Domain.Enums.ReservationStatus.Confirmed ||
                         x.Status == Domain.Enums.ReservationStatus.CheckedIn) &&
                        x.CheckInDate < checkOut &&
                        x.CheckOutDate > checkIn)
            .Select(x => x.SeatId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var blackedOutSeatIds = await _context.Blackouts
            .Where(b => b.StartDate < checkOut && b.EndDate > checkIn)
            .Select(b => b.SeatId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var unavailableIds = reservedSeatIds.Union(blackedOutSeatIds).ToHashSet();

        return await _context.Seats
            .Include(s => s.Tariffs)
            .Where(s => s.AccommodationId == accommodationId &&
                        s.IsActive &&
                        s.Capacity >= minCapacity &&
                        !unavailableIds.Contains(s.Id))
            .OrderBy(s => s.Capacity)
            .ThenBy(s => s.SeatNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<string>> GetOccupiedDatesAsync(
        IEnumerable<Guid> seatIds, DateOnly from, DateOnly until,
        CancellationToken cancellationToken = default)
    {
        var ids = seatIds.ToList();
        if (ids.Count == 0) return FrozenSet<string>.Empty;

        var ranges = await _context.ReservationItems
            .Join(_context.Reservations,
                ri => ri.ReservationId,
                r  => r.Id,
                (ri, r) => new { ri.SeatId, r.Status, r.CheckInDate, r.CheckOutDate })
            .Where(x => ids.Contains(x.SeatId) &&
                        (x.Status == ReservationStatus.Confirmed || x.Status == ReservationStatus.CheckedIn) &&
                        x.CheckOutDate > from &&
                        x.CheckInDate  < until)
            .Select(x => new { x.CheckInDate, x.CheckOutDate })
            .ToListAsync(cancellationToken);

        var occupied = new HashSet<string>();
        foreach (var r in ranges)
            for (var d = r.CheckInDate; d < r.CheckOutDate; d = d.AddDays(1))
                occupied.Add(d.ToString("yyyy-MM-dd"));

        return occupied;
    }
}
