using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace TiefBerg.Diagnostics;

// Zeitmessung pro Klasse.Methode als Aufwandsübersicht (Abschnitt 10). Thread-sicher und ohne Allokation pro Messung.
// Budgetüberschreitungen werden sofort als Warning geloggt (Abschnitt 12), der Rest gesammelt über LogSummary().
//
// Verwendung:  using (Perf.Measure(PerfBudgets.TickMs)) { ... }
public static class Perf
{
    // Höchstens eine Warnung pro ID und Sekunde, damit ein dauerhaft zu langsamer Tick das Log nicht flutet
    private static readonly long WarnIntervalTicks = Stopwatch.Frequency;

    // Schlüssel aus Caller-Infos: Compile-Zeit-Konstanten, daher keine String-Erzeugung pro Messung
    private static readonly ConcurrentDictionary<(string File, string Member), PerfStats> StatsById = new();

    private static ILogger _log;

    public static void Initialize(ILogger log)
    {
        _log = log;
    }

    // Loggt die letzte Übersicht und trennt den Logger; Messungen danach werden nur noch gezählt
    public static void Shutdown()
    {
        LogSummary();
        _log = null;
    }

    // ID = Dateiname.Methode; Dateiname = Klassenname laut Stilregeln
    public static PerfScope Measure(double budgetMs = 0, [CallerFilePath] string file = "",
        [CallerMemberName] string member = "")
    {
        PerfStats stats = StatsById.GetOrAdd((file, member),
            static key => new PerfStats($"{Path.GetFileNameWithoutExtension(key.File)}.{key.Member}"));
        return new PerfScope(stats, budgetMs);
    }

    internal static void Record(PerfStats stats, double budgetMs, long elapsedTicks)
    {
        stats.Add(elapsedTicks);

        if (budgetMs <= 0)
        {
            return;
        }

        double elapsedMs = ToMs(elapsedTicks);
        if (elapsedMs <= budgetMs)
        {
            return;
        }

        Interlocked.Increment(ref stats.OverBudget);
        long now = Stopwatch.GetTimestamp();
        long lastWarn = Volatile.Read(ref stats.LastWarnTimestamp);
        if (now - lastWarn < WarnIntervalTicks
            || Interlocked.CompareExchange(ref stats.LastWarnTimestamp, now, lastWarn) != lastWarn)
        {
            return;
        }

        _log?.Warn($"Budget überschritten: {elapsedMs:F3} ms (Budget {budgetMs:F3} ms)", member: stats.Id);
    }

    // Übersicht seit dem letzten Aufruf, sortiert nach Gesamtaufwand; setzt die Zähler zurück
    public static void LogSummary()
    {
        var rows = StatsById.Values
            .Select(s => s.TakeSnapshot())
            .Where(r => r.Count > 0)
            .OrderByDescending(r => r.TotalTicks)
            .ToList();

        ILogger log = _log;
        if (rows.Count == 0 || log == null || !log.IsEnabled(LogLevel.Information))
        {
            return;
        }

        var text = new StringBuilder("Aufwandsübersicht:");
        foreach (PerfSnapshot row in rows)
        {
            text.AppendLine();
            text.Append($"  {row.Id,-40} n={row.Count,8}  Σ={ToMs(row.TotalTicks),10:F1} ms  " +
                        $"Ø={ToMs(row.TotalTicks) / row.Count,8:F3} ms  max={ToMs(row.MaxTicks),8:F3} ms  " +
                        $"über Budget={row.OverBudget}");
        }

        log.Info(text.ToString());
    }

    private static double ToMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
}

internal readonly record struct PerfSnapshot(string Id, long Count, long TotalTicks, long MaxTicks, long OverBudget);

internal sealed class PerfStats
{
    public readonly string Id;
    public long Count;
    public long TotalTicks;
    public long MaxTicks;
    public long OverBudget;
    public long LastWarnTimestamp;

    public PerfStats(string id)
    {
        Id = id;
    }

    public void Add(long elapsedTicks)
    {
        Interlocked.Increment(ref Count);
        Interlocked.Add(ref TotalTicks, elapsedTicks);

        long max = Volatile.Read(ref MaxTicks);
        while (elapsedTicks > max)
        {
            long previous = Interlocked.CompareExchange(ref MaxTicks, elapsedTicks, max);
            if (previous == max)
            {
                break;
            }

            max = previous;
        }
    }

    // Felder werden einzeln zurückgesetzt; parallel laufende Messungen können um eine Zeile verrutschen, für die Übersicht egal
    public PerfSnapshot TakeSnapshot() => new(
        Id,
        Interlocked.Exchange(ref Count, 0),
        Interlocked.Exchange(ref TotalTicks, 0),
        Interlocked.Exchange(ref MaxTicks, 0),
        Interlocked.Exchange(ref OverBudget, 0));
}
