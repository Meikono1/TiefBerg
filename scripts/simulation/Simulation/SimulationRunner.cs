using System;
using Microsoft.Extensions.Logging;
using TiefBerg.Diagnostics;
using TiefBerg.Simulation.Scheduling;
using TiefBerg.Simulation.Time;

namespace TiefBerg.Simulation;

// Rechnet die Welt bis zu einer Zielzeit weiter (Abschnitt 2/3). Springt von Fälligkeit zu Fälligkeit statt Sekunde für Sekunde,
// Sekunden ohne fälliges System kosten nichts. Ein Aufruf endet nach einem Zeitbudget und setzt beim nächsten fort,
// damit auch Sprünge über Jahre Befehle und Snapshots nicht blockieren. Das Ergebnis hängt nicht vom Budget ab.
// Nicht thread-sicher; läuft im Simulations-Thread (SimulationThread).
public sealed class SimulationRunner
{
    private readonly ILogger<SimulationRunner> _log;
    private readonly TimeProvider _time;

    public SimulationRunner(ILogger<SimulationRunner> log, World world, TimeProvider time)
    {
        _log = log;
        _time = time;
        World = world;
    }

    public World World { get; }

    public WorldClock Clock => World.Clock;

    public Scheduler Scheduler => World.Scheduler;

    // Ein Tick = 1 Sekunde Weltzeit, die feinste Auflösung (Abschnitt 3); eine Kampfrunde sind 6 Ticks (Abschnitt 7)
    public const ulong SecondsPerTick = 1;

    // true: Zielzeit erreicht. false: Budget vorher aufgebraucht, der nächste Aufruf macht weiter.
    public bool AdvanceTo(ulong targetSeconds, double budgetMs)
    {
        ulong now = Clock.Now;
        if (targetSeconds < now)
        {
            // Die Weltzeit läuft nie zurück; TimeControl liefert nie ein Ziel in der Vergangenheit
            _log.Error($"Zielzeit {WorldCalendar.Format(targetSeconds)} liegt vor der Weltzeit {WorldCalendar.Format(now)}, ignoriert.");
            return true;
        }

        long start = _time.GetTimestamp();
        using (Perf.Measure(PerfBudgets.TickMs))
        {
            while (Scheduler.NextDue is ulong due && due <= targetSeconds)
            {
                // Nach dem Laden können Fälligkeiten vor der Weltzeit liegen; sie werden ohne Zeitsprung nachgeholt
                if (due > Clock.Now)
                {
                    Clock.Advance(due - Clock.Now);
                }

                Scheduler.RunDue(Clock.Now);

                if (_time.GetElapsedTime(start).TotalMilliseconds >= budgetMs)
                {
                    return false;
                }
            }

            if (Clock.Now < targetSeconds)
            {
                Clock.Advance(targetSeconds - Clock.Now);
            }
        }

        return true;
    }
}
