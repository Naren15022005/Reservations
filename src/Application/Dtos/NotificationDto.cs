namespace FODUN.Reservations.Application.Dtos;

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    Guid? ReservationId,
    DateTime CreatedAt
);
