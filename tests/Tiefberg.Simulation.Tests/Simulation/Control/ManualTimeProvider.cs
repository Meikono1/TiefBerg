using System;

namespace TiefBerg.Tests.Simulation.Control;

// Echtzeit zum Weiterdrehen per Hand, damit Tests nicht schlafen müssen. Nur für einen Thread.
internal sealed class ManualTimeProvider : TimeProvider
{
    private long _ticks;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp() => _ticks;

    public void Advance(TimeSpan duration) => _ticks += duration.Ticks;
}
