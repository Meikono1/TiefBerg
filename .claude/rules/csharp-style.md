---
paths:
  - "**/*.cs"
---

# C#-Stil

Orientierung am bestehenden Code in `scripts/core/` (Referenz: `GameManager.cs`, `LogManager.cs`).

## Formatierung
- 4 Leerzeichen, Allman-Klammern (öffnende `{` auf eigener Zeile), auch bei einzeiligen `if`-Blöcken.
- File-scoped Namespaces: `namespace TiefBerg.Core;`.
- Namespace folgt dem Ordner: `scripts/core/logging/` → `TiefBerg.Core.Logging`, `scripts/ui/` → `TiefBerg.UI`. Im Simulationskern entfällt `simulation/`: `scripts/simulation/Data/Species/` → `TiefBerg.Data.Species`. Kein Code im globalen Namespace.
- `using` statt vollqualifizierter Namen (Ausnahme: echte Namenskonflikte wie `System.Environment` vs. `Godot.Environment`).

## Benennung
- Klassen, Methoden, Properties, Konstanten: `PascalCase`.
- Private Felder: `_camelCase`. Statische readonly Felder: `PascalCase` (z. B. `Log`, `WriteLock`).
- Godot-Signal-Handler: `On<Quelle><Ereignis>`, z. B. `OnStartPressed`.

## Kommentare und Texte
- Kommentare auf Deutsch, sparsam: das *Warum* erklären, nicht das Offensichtliche.
- Log-Meldungen auf Deutsch (Entwickler-Log).
- Spielertexte auf Englisch und nie im Code. Immer über Übersetzungsschlüssel (Dokument Abschnitt 12).

## Logging (Pflicht in jeder Klasse)
Godot-Seite (`scripts/core/`, `scripts/ui/`):
```csharp
private static readonly GameLogger Log = LogManager.GetLogger<KlassenName>();
```
Simulationskern (`scripts/simulation/`): `ILogger<T>` aus `Microsoft.Extensions.Logging` per Konstruktor, kein statischer Logger, kein `GameLogger`.
```csharp
private readonly ILogger<Scheduler> _log;

public Scheduler(ILogger<Scheduler> log)
{
    _log = log;
}
```
- Aufruf im Kern: `_log.Debug/Info/Warn/Error/Fatal("…")` aus `TiefBerg.Diagnostics.LoggerCallerExtensions`. `LogDebug()`, `LogInformation()` & Co. sind per `scripts/simulation/BannedSymbols.txt` gesperrt (Build-Fehler RS0030), weil sie die Methode nicht mitliefern.
- Der Host erzeugt die Logger über `LogManager.SimulationLoggerFactory`. Systemzeit, Klasse.Methode und Weltzeit stehen automatisch in jeder Zeile; nicht selbst in die Meldung schreiben.
- In Hot Paths (pro Tick, pro Person) `[LoggerMessage]`-Methoden (Source Generator) verwenden. Interpolierte Strings werden sonst auch bei deaktiviertem Level gebaut.
- Mapping: `Trace`/`Debug` → Debug, `Information` → Info, `Warning` → Warn, `Error` → Error, `Critical` → Fatal.

Logzeilen-Format (beide Seiten): `[Systemzeit] [Level] [Klasse.Methode] [T=Weltzeit, nur Kern] Meldung`.

## Zeitmessung
- Zeitkritische Abläufe (Tick, System-Updates, große Schleifen) mit `using (Perf.Measure(PerfBudgets.X)) { … }` messen. Die ID Klasse.Methode wird automatisch gesetzt.
- Budgets nur als Konstanten in `PerfBudgets`, keine Zahlen im Aufruf. `PerfBudgets.TickMs = 8.0` (Ziel 120 FPS).
- Überschreitung → sofort `Warn` (max. 1 pro ID und Sekunde); Übersicht mit Anzahl, Σ, Ø, Max und Überschreitungen alle 10 s über `GameManager`.
- `Perf.Measure()` ohne Budget zählt nur für die Übersicht.
- Level: `Error` = Invariantenverletzung, `Warn` = Budgetüberschreitung oder erwartbarer Fehlerfall, `Info` = System- und Szenariostarts, `Debug` = Würfe und Entscheidungen.
- Teure Meldungen nur hinter `if (Log.IsDebugEnabled)` bauen; Logging darf in Hot Paths kaum Leistung kosten.
- Exceptions als Parameter mitgeben (`Log.Error("…", e)`), nicht nur `e.Message`.

## Umfang
- Dateien: Soft-Limit 500, Hard-Limit 1000 Zeilen. Vorher aufteilen.
- Lieber `// TODO: <was fehlt> (Abschnitt N)` als halb durchdachte Methoden.
- Keine Fallback-Pfade, die Fehler verschleiern: Fehlt etwas, loggen und sauber abbrechen (Muster: `GameManager.ChangeScene`).
