namespace TiefBerg.Diagnostics;

// Zeitbudgets in Millisekunden für Perf.Measure(); Überschreitung = Warning (Abschnitt 12)
public static class PerfBudgets
{
    // 120 FPS = 8,33 ms pro Frame; ein Simulations-Tick muss sicher darunter bleiben
    public const double TickMs = 8.0;

    // Ab hier beendet der Simulations-Thread eine Rechenscheibe und nimmt Befehle an. Unter TickMs, damit nur ein
    // einzelner zu langsamer Systemlauf das Tick-Budget reißt, nicht das normale Ende einer Scheibe.
    public const double SliceMs = 6.0;
}
