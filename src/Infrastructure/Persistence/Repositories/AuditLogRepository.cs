using FODUN.Reservations.Domain.Aggregates;
using FODUN.Reservations.Domain.Interfaces;

namespace FODUN.Reservations.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly ReservationsDbContext _context;

    public AuditLogRepository(ReservationsDbContext context) => _context = context;

    public async Task AddAsync(AuditLog log, CancellationToken cancellationToken = default) =>
        await _context.AuditLogs.AddAsync(log, cancellationToken);
}
