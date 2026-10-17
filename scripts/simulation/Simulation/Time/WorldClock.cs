using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using TiefBerg.Diagnostics;

namespace TiefBerg.Simulation.Time;

// Die eine Weltzeit der Simulation: Sekunden seit Weltstart (Abschnitt 3).
// Geschrieben wird nur vom Simulations-Thread; gelesen wird auch von anderen Threads (Logging, später Snapshots), daher Volatile.
public sealed class WorldClock
{
    private readonly ILogger<WorldClock> _log;
    private ulong _now;

    public WorldClock(ILogger<WorldClock> log, ulong startSeconds = 0)
    {
        _log = log;
        _now = startSeconds;
    }

    public ulong Now => Volatile.Read(ref _now);

    public CalendarDate Date => WorldCalendar.ToDate(Now);

    public void Advance(ulong seconds)
    {
        ulong now = _now;
        if (seconds > ulong.MaxValue - now)
        {
            // Praktisch unerreichbar (~9 * 10^11 Jahre), wäre aber eine Invariantenverletzung
            var e = new InvalidOperationException($"Weltzeit-Überlauf: {now} + {seconds} Sekunden.");
            _log.Error("Weltzeit kann nicht weiterlaufen.", e);
            throw e;
        }

        Volatile.Write(ref _now, now + seconds);
    }

    // Setzt die Weltzeit hart, z. B. beim Laden eines Spielstands (T-012)
    public void Set(ulong seconds)
    {
        ulong previous = _now;
        Volatile.Write(ref _now, seconds);
        _log.Info($"Weltzeit gesetzt: {WorldCalendar.Format(previous)} -> {WorldCalendar.Format(seconds)}");
    }
}
