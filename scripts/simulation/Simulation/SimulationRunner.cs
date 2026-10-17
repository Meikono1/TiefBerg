using Microsoft.Extensions.Logging;
using TiefBerg.Diagnostics;
using TiefBerg.Simulation.Scheduling;
using TiefBerg.Simulation.Time;

namespace TiefBerg.Simulation;

// Einstieg in die tick-basierte Simulation (Abschnitt 2/3). Godot ruft nur Start/Tick/Stop auf.
public sealed class SimulationRunner
{
    private readonly ILogger<SimulationRunner> _log;

    public SimulationRunner(ILogger<SimulationRunner> log, WorldClock clock, Scheduler scheduler)
    {
        _log = log;
        Clock = clock;
        Scheduler = scheduler;
    }

    public WorldClock Clock { get; }

    public Scheduler Scheduler { get; }

    // Ein Tick = 1 Sekunde Weltzeit (Abschnitt 3); eine Kampfrunde sind 6 Ticks (Abschnitt 7)
    public const ulong SecondsPerTick = 1;

    // TODO: Zeit in Tagen/Monaten/Jahren laufen lassen, per Sprung zur nächsten Fälligkeit statt Sekunde für Sekunde (T-022, Abschnitt 3)

    public bool IsRunning { get; private set; }

    public ulong TickCount { get; private set; }

    public void Start()
    {
        if (IsRunning)
        {
            _log.Warn("Simulation läuft bereits.");
            return;
        }

        // TODO: Seed und Weltzustand übergeben, sobald die Welt existiert; die Weltzeit setzt der Aufrufer über Clock (Abschnitt 2, T-012)
        TickCount = 0;
        IsRunning = true;
        _log.Info("Simulation gestartet.");
    }

    public void Tick()
    {
        if (!IsRunning)
        {
            return;
        }

        using (Perf.Measure(PerfBudgets.TickMs))
        {
            TickCount++;
            Clock.Advance(SecondsPerTick);
            Scheduler.RunDue(Clock.Now);
        }
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        IsRunning = false;
        _log.Info($"Simulation beendet nach {TickCount} Ticks.");
    }
}
