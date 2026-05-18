using FODUN.Reservations.Domain.Aggregates;

namespace FODUN.Reservations.Domain.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken cancellationToken = default);
}
