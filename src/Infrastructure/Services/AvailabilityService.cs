using FODUN.Reservations.Application.Common;
using FODUN.Reservations.Application.Dtos;
using FODUN.Reservations.Application.Services;

namespace FODUN.Reservations.Infrastructure.Services;

public sealed class AvailabilityService : IAvailabilityService
{
    private readonly IStoredProcedureRepository _spRepository;

    public AvailabilityService(IStoredProcedureRepository spRepository) =>
        _spRepository = spRepository;

    public async Task<Result<IEnumerable<AvailableRoomDto>>> GetAvailableRoomsAsync(
        Guid accommodationId,
        DateOnly checkIn,
        DateOnly checkOut,
        int totalPersons,
        CancellationToken cancellationToken = default)
    {
        if (checkIn >= checkOut)
            return Result<IEnumerable<AvailableRoomDto>>.Failure(
                "El check-in debe ser anterior al check-out.");

        if (totalPersons <= 0)
            return Result<IEnumerable<AvailableRoomDto>>.Failure(
                "El número de personas debe ser mayor a cero.");

        var seats = await _spRepository.GetAvailableRoomsByDateRangeAndPersonsAsync(
            accommodationId, checkIn, checkOut, totalPersons, cancellationToken);

        var dtos = new List<AvailableRoomDto>(seats.Count);
        foreach (var seat in seats)
        {
            var tariffs = await _spRepository.GetApplicableTariffsAsync(
                seat.SeatId, checkIn, totalPersons, cancellationToken);

            var tariff = tariffs.FirstOrDefault();

            dtos.Add(new AvailableRoomDto(
                seat.SeatId,
                seat.SeatNumber,
                seat.Type,
                seat.Capacity,
                seat.Description,
                seat.Amenities,
                tariff?.PricePerNight ?? 0m,
                tariff?.Season ?? "Sin tarifa"));
        }

        return Result<IEnumerable<AvailableRoomDto>>.Success(dtos);
    }
}
