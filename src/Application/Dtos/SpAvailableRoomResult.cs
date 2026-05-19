namespace FODUN.Reservations.Application.Dtos;

/// <summary>
/// Proyección de fila devuelta por sp_GetAvailableRooms_ByDateRange
/// y sp_GetAvailableRooms_ByDateRangeAndPersons.
/// Las columnas adicionales de SP2 son opcionales.
/// </summary>
public sealed class SpAvailableRoomResult
{
    public Guid   SeatId      { get; init; }
    public string SeatNumber  { get; init; } = string.Empty;
    public string Type        { get; init; } = string.Empty;
    public int    Capacity    { get; init; }
    public string? Description { get; init; }
    public string? Amenities   { get; init; }
    public int    Nights      { get; init; }

    // Solo presente en SP2 (disponibilidad por personas)
    public int?  RoomsNeededForAllPersons { get; init; }
    public bool? FitsInOneRoom            { get; init; }
}
