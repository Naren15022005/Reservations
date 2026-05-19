using FODUN.Reservations.Application.Common;
using FODUN.Reservations.Application.Dtos;

namespace FODUN.Reservations.Application.Services;

public interface INotificationService
{
    Task<Result<IEnumerable<NotificationDto>>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task CreateAsync(Guid userId, string title, string message, string type, Guid? reservationId = null, CancellationToken cancellationToken = default);
}
