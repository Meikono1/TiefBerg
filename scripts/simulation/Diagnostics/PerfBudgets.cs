namespace TiefBerg.Diagnostics;

// Zeitbudgets in Millisekunden für Perf.Measure(); Überschreitung = Warning (Abschnitt 12)
public static class PerfBudgets
{
    // 120 FPS = 8,33 ms pro Frame; ein Simulations-Tick muss sicher darunter bleiben
    public const double TickMs = 8.0;
}
