namespace FODUN.Reservations.Domain.ValueObjects;

public sealed class DateRange : IEquatable<DateRange>
{
    public DateOnly CheckIn { get; }
    public DateOnly CheckOut { get; }
    public int Nights => CheckOut.DayNumber - CheckIn.DayNumber;

    public DateRange(DateOnly checkIn, DateOnly checkOut)
    {
        if (checkIn >= checkOut)
            throw new ArgumentException("La fecha de check-in debe ser anterior al check-out.");
        if (checkIn < DateOnly.FromDateTime(DateTime.UtcNow.Date))
            throw new ArgumentException("La fecha de check-in no puede ser en el pasado.");
        CheckIn = checkIn;
        CheckOut = checkOut;
    }

    public bool OverlapsWith(DateRange other) =>
        CheckIn < other.CheckOut && CheckOut > other.CheckIn;

    public bool Contains(DateOnly date) => date >= CheckIn && date < CheckOut;

    public bool Equals(DateRange? other) =>
        other is not null && CheckIn == other.CheckIn && CheckOut == other.CheckOut;

    public override bool Equals(object? obj) => obj is DateRange d && Equals(d);
    public override int GetHashCode() => HashCode.Combine(CheckIn, CheckOut);
    public override string ToString() => $"{CheckIn:dd/MM/yyyy} → {CheckOut:dd/MM/yyyy} ({Nights} noches)";
}
