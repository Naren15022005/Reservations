using FODUN.Reservations.Application.Common;
using FODUN.Reservations.Application.Dtos;
using FODUN.Reservations.Application.Services;

namespace FODUN.Reservations.Infrastructure.Services;

public sealed class TariffService : ITariffService
{
    private readonly IStoredProcedureRepository _spRepository;

    public TariffService(IStoredProcedureRepository spRepository) =>
        _spRepository = spRepository;

    public async Task<Result<ReservationCostDto>> CalculateCostAsync(
        Guid seatId,
        DateOnly checkIn,
        DateOnly checkOut,
        int totalPersons,
        bool includeLaundry,
        CancellationToken cancellationToken = default)
    {
        if (checkIn >= checkOut)
            return Result<ReservationCostDto>.Failure("El rango de fechas no es válido.");

        if (totalPersons <= 0)
            return Result<ReservationCostDto>.Failure(
                "El número de personas debe ser mayor a cero.");

        var cost = await _spRepository.CalculateReservationCostAsync(
            seatId, checkIn, checkOut, totalPersons, includeLaundry, cancellationToken);

        if (cost is null)
            return Result<ReservationCostDto>.Failure(
                "No hay tarifa disponible para la selección indicada.");

        decimal baseTotal       = cost.PricePerNight * cost.Nights;
        decimal additionalTotal = cost.AdditionalPersonPrice * cost.AdditionalPersons * cost.Nights;

        return Result<ReservationCostDto>.Success(new ReservationCostDto(
            baseTotal,
            additionalTotal,
            cost.LaundryServiceCost,
            cost.TotalCost,
            cost.Nights,
            cost.AdditionalPersons));
    }
}
