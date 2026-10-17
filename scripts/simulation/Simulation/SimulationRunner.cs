using Microsoft.Extensions.Logging;
using TiefBerg.Diagnostics;

namespace TiefBerg.Simulation;

// Einstieg in die tick-basierte Simulation (Abschnitt 2/3). Godot ruft nur Start/Tick/Stop auf.
public sealed class SimulationRunner
{
    private readonly ILogger<SimulationRunner> _log;

    public SimulationRunner(ILogger<SimulationRunner> log)
    {
        _log = log;
    }

    public bool IsRunning { get; private set; }

    public ulong TickCount { get; private set; }

    public void Start()
    {
        if (IsRunning)
        {
            _log.Warn("Simulation läuft bereits.");
            return;
        }

        // TODO: Seed und Weltzustand übergeben, sobald Weltzeit und Welt existieren (T-009, Abschnitt 2)
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
            // TODO: Weltzeit fortschreiben (T-009)
            // TODO: Fällige Systeme über den Scheduler abarbeiten (T-010)
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
