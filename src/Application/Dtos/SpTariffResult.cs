namespace FODUN.Reservations.Application.Dtos;

/// <summary>
/// Proyección de fila devuelta por sp_GetApplicableTariffs.
/// ValidFrom / ValidUntil vienen de columnas DATE → se leen como DateTime.
/// </summary>
public sealed class SpTariffResult
{
    public Guid     Id           { get; init; }
    public Guid     SeatId       { get; init; }
    public string   Season       { get; init; } = string.Empty;
    public int      MinPersons   { get; init; }
    public int      MaxPersons   { get; init; }
    public decimal  PricePerNight         { get; init; }
    public decimal? AdditionalPersonPrice { get; init; }
    public string?  DaysOfWeek   { get; init; }
    public bool     IsExceptional { get; init; }
    public DateTime ValidFrom    { get; init; }
    public DateTime? ValidUntil  { get; init; }
    public bool     IsSpecialApplicableToday { get; init; }
}
