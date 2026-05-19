using FODUN.Reservations.Domain.ValueObjects;
using FODUN.Reservations.Domain.Aggregates.User;
using Xunit;

namespace FODUN.Reservations.Tests.Unit;

public class MoneyTests
{
    [Fact]
    public void Money_NegativeAmount_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Money(-1));
    }

    [Fact]
    public void Money_Add_ReturnsCorrectSum()
    {
        var a = new Money(50_000);
        var b = new Money(18_000);
        Assert.Equal(new Money(68_000), a.Add(b));
    }

    [Fact]
    public void Money_Multiply_ReturnsCorrectResult()
    {
        var price = new Money(70_000);
        Assert.Equal(new Money(210_000), price.Multiply(3));
    }
}

public class DateRangeTests
{
    [Fact]
    public void DateRange_SameDates_ThrowsArgumentException()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        Assert.Throws<ArgumentException>(() => new DateRange(date, date));
    }

    [Fact]
    public void DateRange_ValidRange_CalculatesNightsCorrectly()
    {
        var checkIn  = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var checkOut = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(8));
        var range = new DateRange(checkIn, checkOut);
        Assert.Equal(3, range.Nights);
    }

    [Fact]
    public void DateRange_OverlapsWith_DetectsConflict()
    {
        var r1 = new DateRange(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)));
        var r2 = new DateRange(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(8)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)));
        Assert.True(r1.OverlapsWith(r2));
    }
}

public class UserTests
{
    [Fact]
    public void User_Create_WithValidData_ReturnsUser()
    {
        var user = User.Create("12345678", "Juan Pérez", "juan@test.com", "hashedPw", "salt", "3001234567");
        Assert.NotNull(user);
        Assert.Equal("juan@test.com", user.Email);
        Assert.False(user.IsEmailConfirmed);
        Assert.NotNull(user.EmailConfirmationToken);
    }

    [Fact]
    public void User_RegisterFailedLogin_LocksAfterFiveAttempts()
    {
        var user = User.Create("12345678", "Juan Pérez", "juan@test.com", "hashedPw", "salt");
        for (int i = 0; i < 5; i++) user.RegisterFailedLogin();
        Assert.True(user.IsLockedOut());
    }

    [Fact]
    public void User_Create_EmptyEmail_ThrowsDomainException()
    {
        Assert.Throws<FODUN.Reservations.Domain.Exceptions.DomainException>(
            () => User.Create("12345678", "Juan Pérez", "", "hash", "salt"));
    }
}
