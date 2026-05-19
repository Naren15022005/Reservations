using FODUN.Reservations.Application.Dtos;

namespace FODUN.Reservations.Application.Services;

public interface IStoredProcedureRepository
{
    /// <summary>sp_GetAvailableRooms_ByDateRange — habitaciones libres en el rango.</summary>
    Task<IReadOnlyList<SpAvailableRoomResult>> GetAvailableRoomsByDateRangeAsync(
        Guid   accommodationId,
        DateOnly checkIn,
        DateOnly checkOut,
        int    minCapacity,
        CancellationToken cancellationToken = default);

    /// <summary>sp_GetAvailableRooms_ByDateRangeAndPersons — libres y con capacidad suficiente.</summary>
    Task<IReadOnlyList<SpAvailableRoomResult>> GetAvailableRoomsByDateRangeAndPersonsAsync(
        Guid   accommodationId,
        DateOnly checkIn,
        DateOnly checkOut,
        int    totalPersons,
        CancellationToken cancellationToken = default);

    /// <summary>sp_GetApplicableTariffs — tarifas ordenadas por prioridad.</summary>
    Task<IReadOnlyList<SpTariffResult>> GetApplicableTariffsAsync(
        Guid   seatId,
        DateOnly checkIn,
        int    totalPersons,
        CancellationToken cancellationToken = default);

    /// <summary>sp_CalculateReservationCost — costo total con desglose.</summary>
    Task<SpCostResult?> CalculateReservationCostAsync(
        Guid   seatId,
        DateOnly checkIn,
        DateOnly checkOut,
        int    totalPersons,
        bool   includeLaundry,
        CancellationToken cancellationToken = default);
}
