namespace FODUN.Reservations.Application.Dtos;

/// <summary>
/// Proyección del SELECT de resultado devuelto por sp_CalculateReservationCost.
/// </summary>
public sealed class SpCostResult
{
    public decimal TotalCost             { get; init; }
    public int     Nights                { get; init; }
    public int     AdditionalPersons     { get; init; }
    public decimal PricePerNight         { get; init; }
    public decimal AdditionalPersonPrice { get; init; }
    public decimal LaundryServiceCost    { get; init; }
}
