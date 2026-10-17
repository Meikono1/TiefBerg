using System;
using Microsoft.Extensions.Logging.Abstractions;
using TiefBerg.Simulation.Time;

namespace TiefBerg.Tests.Simulation.Time;

public class WorldClockTests
{
    private static WorldClock CreateClock(ulong start = 0) => new(NullLogger<WorldClock>.Instance, start);

    [Fact]
    public void Advance_AddsSeconds()
    {
        WorldClock clock = CreateClock(10);

        clock.Advance(5);

        Assert.Equal(15UL, clock.Now);
    }

    [Fact]
    public void Advance_PastMaxValue_ThrowsAndKeepsTime()
    {
        WorldClock clock = CreateClock(ulong.MaxValue - 1);

        Assert.Throws<InvalidOperationException>(() => clock.Advance(2));
        Assert.Equal(ulong.MaxValue - 1, clock.Now);
    }

    [Fact]
    public void Set_ReplacesTime()
    {
        WorldClock clock = CreateClock(100);

        clock.Set(WorldCalendar.SecondsPerDay);

        Assert.Equal(new CalendarDate(1, 1, 2, 0, 0, 0), clock.Date);
    }
}
