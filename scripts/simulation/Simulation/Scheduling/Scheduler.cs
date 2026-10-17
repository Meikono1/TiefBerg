using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TiefBerg.Diagnostics;

namespace TiefBerg.Simulation.Scheduling;

// Arbeitet Systeme nach Fälligkeit ab (Abschnitt 3): eine nach Fälligkeit sortierte Warteschlange statt Polling pro Tick.
// Gleiche Fälligkeit -> Reihenfolge des Einreihens (laufende Nummer), damit der Ablauf deterministisch bleibt (Abschnitt 2).
// Nicht thread-sicher; läuft im Simulations-Thread.
public sealed class Scheduler
{
    private readonly ILogger<Scheduler> _log;
    private readonly PriorityQueue<Entry, (ulong Due, ulong Sequence)> _queue = new();
    private ulong _nextSequence;

    public Scheduler(ILogger<Scheduler> log)
    {
        _log = log;
    }

    // TODO: Systeme parallel abarbeiten, wenn sie unabhängig sind (Multicore, Abschnitt 2)
    // TODO: Freie Kapazität für vorgezogene Berechnungen nutzen (Abschnitt 3); wann Vorgezogenes ungültig wird, ist offen
    // TODO: Systeme abmelden, sobald es dafür einen Anlass gibt

    public int Count => _queue.Count;

    // Nächste Fälligkeit, null ohne Systeme. Grundlage für Zeitsprünge "bis zum nächsten Ereignis" (T-022).
    public ulong? NextDue => _queue.TryPeek(out _, out (ulong Due, ulong Sequence) key) ? key.Due : null;

    public void Register(ISimulationSystem system, ulong firstDueSeconds)
    {
        string name = system.GetType().Name;
        if (system.IntervalSeconds == 0)
        {
            var e = new ArgumentOutOfRangeException(nameof(system), "IntervalSeconds muss größer als 0 sein.");
            _log.Error($"System {name} nicht registriert.", e);
            throw e;
        }

        Enqueue(new Entry(system, name), firstDueSeconds);
        _log.Info($"System {name} registriert: alle {system.IntervalSeconds} s, zuerst bei {firstDueSeconds}.");
    }

    // Führt alle Läufe mit Fälligkeit <= nowSeconds aus, auch mehrere desselben Systems nach einem Zeitsprung
    public void RunDue(ulong nowSeconds)
    {
        while (_queue.TryPeek(out Entry? entry, out (ulong Due, ulong Sequence) key) && key.Due <= nowSeconds)
        {
            _queue.Dequeue();
            try
            {
                using (Perf.MeasureAs(entry.Name, nameof(ISimulationSystem.Run)))
                {
                    entry.System.Run(key.Due);
                }
            }
            catch (Exception e)
            {
                // Kein Weiterlaufen mit halb aktualisiertem Zustand; das System ist aus der Warteschlange
                _log.Error($"System {entry.Name} ist bei Fälligkeit {key.Due} fehlgeschlagen.", e);
                throw;
            }

            // Ab der alten Fälligkeit weiterzählen, damit der Takt nicht um die Verspätung driftet
            Enqueue(entry, checked(key.Due + entry.System.IntervalSeconds));
        }
    }

    private void Enqueue(Entry entry, ulong due)
    {
        _queue.Enqueue(entry, (due, _nextSequence++));
    }

    private sealed record Entry(ISimulationSystem System, string Name);
}
