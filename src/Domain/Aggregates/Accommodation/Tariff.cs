using FODUN.Reservations.Domain.Aggregates;
using FODUN.Reservations.Domain.Enums;
using FODUN.Reservations.Domain.ValueObjects;

namespace FODUN.Reservations.Domain.Aggregates.Accommodation;

public sealed class Tariff : BaseEntity
{
    public Guid SeatId { get; private set; }
    public SeasonType Season { get; private set; }
    public int MinPersons { get; private set; } = 1;
    public int MaxPersons { get; private set; }
    public decimal PricePerNight { get; private set; }
    public decimal? AdditionalPersonPrice { get; private set; }
    public string? DaysOfWeek { get; private set; }
    public bool IsExceptional { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidUntil { get; private set; }

    private Tariff() { }

    public static Tariff Create(
        Guid seatId,
        SeasonType season,
        int maxPersons,
        decimal pricePerNight,
        DateOnly validFrom,
        int minPersons = 1,
        decimal? additionalPersonPrice = null,
        bool isExceptional = false,
        string? daysOfWeek = null,
        DateOnly? validUntil = null)
    {
        return new Tariff
        {
            SeatId = seatId,
            Season = season,
            MinPersons = minPersons,
            MaxPersons = maxPersons,
            PricePerNight = pricePerNight,
            AdditionalPersonPrice = additionalPersonPrice,
            IsExceptional = isExceptional,
            DaysOfWeek = daysOfWeek,
            ValidFrom = validFrom,
            ValidUntil = validUntil
        };
    }

    public bool AppliesTo(DateOnly date, int persons) =>
        MinPersons <= persons &&
        persons <= MaxPersons &&
        ValidFrom <= date &&
        (ValidUntil is null || ValidUntil >= date);

    public bool IsSpecialWeekdayApplicable(DateOnly checkIn)
    {
        if (!IsExceptional) return false;
        var dow = checkIn.DayOfWeek;
        return dow is DayOfWeek.Monday or DayOfWeek.Tuesday or DayOfWeek.Wednesday or DayOfWeek.Thursday;
    }
}
