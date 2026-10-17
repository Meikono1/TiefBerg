using System;
using System.Diagnostics;

namespace TiefBerg.Diagnostics;

// Misst von der Erzeugung bis Dispose. Struct, damit pro Messung nichts auf dem Heap landet.
public readonly struct PerfScope : IDisposable
{
    private readonly PerfStats? _stats;
    private readonly double _budgetMs;
    private readonly long _start;

    internal PerfScope(PerfStats stats, double budgetMs)
    {
        _stats = stats;
        _budgetMs = budgetMs;
        _start = Stopwatch.GetTimestamp();
    }

    public void Dispose()
    {
        // default(PerfScope) hat keine Stats und misst nichts
        if (_stats != null)
        {
            Perf.Record(_stats, _budgetMs, Stopwatch.GetTimestamp() - _start);
        }
    }
}
