using FODUN.Reservations.Application.Common;
using FODUN.Reservations.Application.Dtos;

namespace FODUN.Reservations.Application.Services;

public interface ITariffService
{
    Task<Result<ReservationCostDto>> CalculateCostAsync(
        Guid seatId,
        DateOnly checkIn,
        DateOnly checkOut,
        int totalPersons,
        bool includeLaundry,
        CancellationToken cancellationToken = default);
}
