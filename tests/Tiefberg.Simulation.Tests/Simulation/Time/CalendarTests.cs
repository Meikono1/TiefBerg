using System;
using TiefBerg.Simulation.Time;

namespace TiefBerg.Tests.Simulation.Time;

public class CalendarTests
{
    private const ulong Year = WorldCalendar.SecondsPerYear;
    private const ulong Epoch = 1000 * Year;

    private static readonly Calendar Human = new("human", Epoch);

    [Fact]
    public void ToDate_AtEpoch_IsFirstDayOfYearOne()
    {
        Assert.Equal(new CalendarDate(1, 1, 1, 0, 0, 0), Human.ToDate(Epoch));
    }

    [Fact]
    public void ToDate_AfterEpoch_CountsFromEpoch()
    {
        Assert.Equal(new CalendarDate(1, 1, 2, 0, 0, 5), Human.ToDate(Epoch + WorldCalendar.SecondsPerDay + 5));
    }

    [Fact]
    public void ToDate_OneSecondBeforeEpoch_IsLastSecondOfYearOneBeforeEra()
    {
        Assert.Equal(new CalendarDate(1, 12, 23, 19, 59, 59, BeforeEra: true), Human.ToDate(Epoch - 1));
    }

    [Fact]
    public void ToDate_ExactlyOneYearBeforeEpoch_IsStartOfYearOneBeforeEra()
    {
        Assert.Equal(new CalendarDate(1, 1, 1, 0, 0, 0, BeforeEra: true), Human.ToDate(Epoch - Year));
    }

    [Fact]
    public void ToDate_JustBeforeYearOneBeforeEra_IsYearTwoBeforeEra()
    {
        Assert.Equal(new CalendarDate(2, 12, 23, 19, 59, 59, BeforeEra: true), Human.ToDate(Epoch - Year - 1));
    }

    [Fact]
    public void ToDate_WorldTimeZero_IsBeforeEra()
    {
        Assert.Equal(new CalendarDate(1000, 1, 1, 0, 0, 0, BeforeEra: true), Human.ToDate(0));
    }

    [Fact]
    public void DifferentEpochs_GiveDifferentDatesForSameWorldTime()
    {
        var dragon = new Calendar("dragon", 3 * WorldCalendar.SecondsPerMonth);
        ulong now = Epoch + 42 * WorldCalendar.SecondsPerDay;

        Assert.Equal("1-02-20 00:00:00", Human.Format(now));
        Assert.Equal("1000-11-20 00:00:00", dragon.Format(now));
        Assert.Equal("1-10-01 00:00:00 v. Z.", Human.Format(Epoch - 3 * WorldCalendar.SecondsPerMonth));
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(Epoch - Year - 1)]
    [InlineData(Epoch - Year)]
    [InlineData(Epoch - 1)]
    [InlineData(Epoch)]
    [InlineData(Epoch + 123_456_789)]
    public void ToWorldSeconds_RoundTrip(ulong worldSeconds)
    {
        Assert.Equal(worldSeconds, Human.ToWorldSeconds(Human.ToDate(worldSeconds)));
    }

    [Fact]
    public void ToWorldSeconds_DateBeforeWorldTimeZero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Human.ToWorldSeconds(new CalendarDate(1001, 1, 1, 0, 0, 0, BeforeEra: true)));
    }

    [Fact]
    public void ToWorldSeconds_YearZeroBeforeEra_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Human.ToWorldSeconds(new CalendarDate(0, 1, 1, 0, 0, 0, BeforeEra: true)));
    }

    [Fact]
    public void WorldCalendar_RejectsBeforeEra()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WorldCalendar.ToSeconds(new CalendarDate(1, 1, 1, 0, 0, 0, BeforeEra: true)));
    }
}
