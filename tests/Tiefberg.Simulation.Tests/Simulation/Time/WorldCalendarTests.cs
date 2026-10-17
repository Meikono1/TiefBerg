using System;
using TiefBerg.Simulation.Time;

namespace TiefBerg.Tests.Simulation.Time;

public class WorldCalendarTests
{
    [Fact]
    public void SecondsPerYear_Matches276DaysOf20Hours()
    {
        Assert.Equal(276UL * 20 * 60 * 60, WorldCalendar.SecondsPerYear);
    }

    [Fact]
    public void ToDate_WorldStart_IsFirstDayOfFirstYear()
    {
        Assert.Equal(new CalendarDate(1, 1, 1, 0, 0, 0), WorldCalendar.ToDate(0));
    }

    [Theory]
    [InlineData(59UL, 1UL, 1, 1, 0, 0, 59)]
    [InlineData(60UL, 1UL, 1, 1, 0, 1, 0)]
    [InlineData(72_000UL - 1, 1UL, 1, 1, 19, 59, 59)] // letzte Sekunde des ersten Tages (20 h)
    [InlineData(72_000UL, 1UL, 1, 2, 0, 0, 0)]
    [InlineData(72_000UL * 23, 1UL, 2, 1, 0, 0, 0)] // Monat hat 23 Tage
    [InlineData(19_872_000UL - 1, 1UL, 12, 23, 19, 59, 59)] // letzte Sekunde des ersten Jahres
    [InlineData(19_872_000UL, 2UL, 1, 1, 0, 0, 0)]
    public void ToDate_Boundaries(ulong seconds, ulong year, int month, int day, int hour, int minute, int second)
    {
        Assert.Equal(new CalendarDate(year, month, day, hour, minute, second), WorldCalendar.ToDate(seconds));
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(123_456_789UL)]
    [InlineData(ulong.MaxValue)]
    public void ToSeconds_RoundTrip(ulong seconds)
    {
        Assert.Equal(seconds, WorldCalendar.ToSeconds(WorldCalendar.ToDate(seconds)));
    }

    [Theory]
    [InlineData(0UL, 1, 1, 0, 0, 0)]
    [InlineData(1UL, 0, 1, 0, 0, 0)]
    [InlineData(1UL, 13, 1, 0, 0, 0)]
    [InlineData(1UL, 1, 24, 0, 0, 0)]
    [InlineData(1UL, 1, 1, 20, 0, 0)]
    [InlineData(1UL, 1, 1, 0, 60, 0)]
    [InlineData(1UL, 1, 1, 0, 0, -1)]
    public void ToSeconds_InvalidDate_Throws(ulong year, int month, int day, int hour, int minute, int second)
    {
        var date = new CalendarDate(year, month, day, hour, minute, second);

        Assert.Throws<ArgumentOutOfRangeException>(() => WorldCalendar.ToSeconds(date));
    }

    [Fact]
    public void ToSeconds_YearBeyondRange_Throws()
    {
        var date = new CalendarDate(ulong.MaxValue, 1, 1, 0, 0, 0);

        Assert.Throws<OverflowException>(() => WorldCalendar.ToSeconds(date));
    }

    [Fact]
    public void Format_PadsMonthDayAndTime()
    {
        ulong seconds = WorldCalendar.ToSeconds(new CalendarDate(1003, 4, 7, 9, 5, 3));

        Assert.Equal("1003-04-07 09:05:03", WorldCalendar.Format(seconds));
    }
}
