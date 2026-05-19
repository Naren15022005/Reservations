using FODUN.Reservations.Application.Common;
using FODUN.Reservations.Application.Dtos;
using FODUN.Reservations.Application.Services;
using FODUN.Reservations.Domain.Aggregates.Notification;
using FODUN.Reservations.Domain.Interfaces;

namespace FODUN.Reservations.Api.Services;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<NotificationDto>>> GetByUserAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _notificationRepository.GetByUserIdAsync(userId, cancellationToken);
        var dtos = notifications.Select(n => new NotificationDto(
            n.Id, n.Title, n.Message, n.Type, n.IsRead, n.ReservationId, n.CreatedAt));
        return Result<IEnumerable<NotificationDto>>.Success(dtos);
    }

    public async Task<int> GetUnreadCountAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await _notificationRepository.GetUnreadCountAsync(userId, cancellationToken);

    public async Task<Result> MarkAllAsReadAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        await _notificationRepository.MarkAllAsReadAsync(userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task CreateAsync(
        Guid userId, string title, string message, string type,
        Guid? reservationId = null, CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(userId, title, message, type, reservationId);
        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
