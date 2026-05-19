using FODUN.Reservations.Domain.Aggregates.Notification;
using FODUN.Reservations.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FODUN.Reservations.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly ReservationsDbContext _context;

    public NotificationRepository(ReservationsDbContext context) => _context = context;

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<int> GetUnreadCountAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default) =>
        await _context.Notifications.AddAsync(notification, cancellationToken);

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var n in unread)
            n.MarkAsRead();
    }
}
