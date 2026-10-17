using System;
using System.Collections.Generic;
using System.Linq;
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
        // Klassenname nur für die Zeitmessung; zugeordnet wird über die Id
        string perfName = system.GetType().Name;
        if (system.IntervalSeconds == 0)
        {
            var e = new ArgumentOutOfRangeException(nameof(system), "IntervalSeconds muss größer als 0 sein.");
            _log.Error($"System {system.Id} nicht registriert.", e);
            throw e;
        }

        if (string.IsNullOrWhiteSpace(system.Id) || _queue.UnorderedItems.Any(item => item.Element.Id == system.Id))
        {
            var e = new InvalidOperationException($"System-Id '{system.Id}' ist leer oder bereits registriert.");
            _log.Error("Registrierung abgelehnt.", e);
            throw e;
        }

        Enqueue(new Entry(system, system.Id, perfName), firstDueSeconds);
        _log.Info($"System {system.Id} registriert: alle {system.IntervalSeconds} s, zuerst bei {firstDueSeconds}.");
    }

    // Führt alle Läufe mit Fälligkeit <= nowSeconds aus, auch mehrere desselben Systems nach einem Zeitsprung
    public void RunDue(ulong nowSeconds)
    {
        while (_queue.TryPeek(out Entry? entry, out (ulong Due, ulong Sequence) key) && key.Due <= nowSeconds)
        {
            _queue.Dequeue();
            try
            {
                using (Perf.MeasureAs(entry.PerfName, nameof(ISimulationSystem.Run)))
                {
                    entry.System.Run(key.Due);
                }
            }
            catch (Exception e)
            {
                // Kein Weiterlaufen mit halb aktualisiertem Zustand; das System ist aus der Warteschlange
                _log.Error($"System {entry.Id} ist bei Fälligkeit {key.Due} fehlgeschlagen.", e);
                throw;
            }

            // Ab der alten Fälligkeit weiterzählen, damit der Takt nicht um die Verspätung driftet
            Enqueue(entry, checked(key.Due + entry.System.IntervalSeconds));
        }
    }

    // Fälligkeiten in Abarbeitungsreihenfolge, für Spielstände (T-012)
    public IReadOnlyList<ScheduledRun> GetSchedule() => _queue.UnorderedItems
        .OrderBy(item => item.Priority.Due)
        .ThenBy(item => item.Priority.Sequence)
        .Select(item => new ScheduledRun(item.Element.Id, item.Priority.Due))
        .ToList();

    // Übernimmt gespeicherte Fälligkeiten für bereits registrierte Systeme (Zuordnung über die Id).
    // Reihenfolge des Spielstands bleibt erhalten, damit Läufe mit gleicher Fälligkeit wie vor dem Speichern laufen.
    public void ApplySchedule(IReadOnlyList<ScheduledRun> schedule)
    {
        // Aktuelle Registrierungen als Liste: keine ergebnisrelevante Iteration über ein Dictionary (Abschnitt 2)
        var current = _queue.UnorderedItems
            .OrderBy(item => item.Priority.Due)
            .ThenBy(item => item.Priority.Sequence)
            .Select(item => (Entry: item.Element, Due: item.Priority.Due))
            .ToList();
        var unassigned = current.ToDictionary(c => c.Entry.Id, c => c.Entry);
        _queue.Clear();

        foreach (ScheduledRun run in schedule)
        {
            if (!unassigned.Remove(run.SystemId, out Entry? entry))
            {
                _log.Warn($"System {run.SystemId} aus dem Spielstand ist nicht registriert, Fälligkeit verworfen.");
                continue;
            }

            Enqueue(entry, run.DueSeconds);
        }

        // Systeme, die der Spielstand noch nicht kannte, behalten ihre Fälligkeit aus der Registrierung
        foreach ((Entry entry, ulong due) in current.Where(c => unassigned.ContainsKey(c.Entry.Id)))
        {
            _log.Warn($"System {entry.Id} fehlt im Spielstand, startet mit Fälligkeit {due}.");
            Enqueue(entry, due);
        }

        _log.Info($"{_queue.Count} Fälligkeiten aus dem Spielstand übernommen.");
    }

    private void Enqueue(Entry entry, ulong due)
    {
        _queue.Enqueue(entry, (due, _nextSequence++));
    }

    private sealed record Entry(ISimulationSystem System, string Id, string PerfName);
}
