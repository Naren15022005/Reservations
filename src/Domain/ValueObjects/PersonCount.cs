namespace FODUN.Reservations.Domain.ValueObjects;

public sealed class PersonCount : IEquatable<PersonCount>
{
    public int Value { get; }

    public PersonCount(int value)
    {
        if (value <= 0)
            throw new ArgumentException("El número de personas debe ser mayor a cero.", nameof(value));
        if (value > 100)
            throw new ArgumentException("El número de personas supera el límite permitido.", nameof(value));
        Value = value;
    }

    public bool ExceedsCapacity(int capacity) => Value > capacity;
    public int AdditionalPersonsOver(int capacity) => Math.Max(0, Value - capacity);

    public bool Equals(PersonCount? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is PersonCount p && Equals(p);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"{Value} persona(s)";

    public static implicit operator int(PersonCount p) => p.Value;
}
