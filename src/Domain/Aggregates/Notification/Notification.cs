using FODUN.Reservations.Domain.Aggregates;

namespace FODUN.Reservations.Domain.Aggregates.Notification;

public sealed class Notification : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public Guid? ReservationId { get; private set; }

    private Notification() { }

    public static Notification Create(
        Guid userId, string title, string message, string type, Guid? reservationId = null) =>
        new()
        {
            UserId = userId,
            Title = title.Trim(),
            Message = message.Trim(),
            Type = type,
            ReservationId = reservationId
        };

    public void MarkAsRead()
    {
        IsRead = true;
        SetUpdated();
    }
}
