using FODUN.Reservations.Application.Common;
using FODUN.Reservations.Application.Dtos;

namespace FODUN.Reservations.Application.Services;

public interface IAvailabilityService
{
    Task<Result<IEnumerable<AvailableRoomDto>>> GetAvailableRoomsAsync(
        Guid accommodationId,
        DateOnly checkIn,
        DateOnly checkOut,
        int totalPersons,
        CancellationToken cancellationToken = default);
}
