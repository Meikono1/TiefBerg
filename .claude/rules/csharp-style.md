---
paths:
  - "**/*.cs"
---

# C#-Stil

Orientierung am bestehenden Code in `scripts/core/` (Referenz: `GameManager.cs`, `LogManager.cs`).

## Formatierung
- 4 Leerzeichen, Allman-Klammern (öffnende `{` auf eigener Zeile), auch bei einzeiligen `if`-Blöcken.
- File-scoped Namespaces: `namespace TiefBerg.Core;`.
- Namespace folgt dem Ordner: `scripts/core/logging/` → `TiefBerg.Core.Logging`, `scripts/ui/` → `TiefBerg.UI`. Kein Code im globalen Namespace.
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
```csharp
private static readonly GameLogger Log = LogManager.GetLogger<KlassenName>();
```
- Level: `Error` = Invariantenverletzung, `Warn` = Budgetüberschreitung oder erwartbarer Fehlerfall, `Info` = System- und Szenariostarts, `Debug` = Würfe und Entscheidungen.
- Teure Meldungen nur hinter `if (Log.IsDebugEnabled)` bauen; Logging darf in Hot Paths kaum Leistung kosten.
- Exceptions als Parameter mitgeben (`Log.Error("…", e)`), nicht nur `e.Message`.
- Simulationskern (ohne Godot): Logger per Konstruktor, Meldungen tragen die Weltzeit (siehe `architecture.md`).

## Umfang
- Dateien: Soft-Limit 500, Hard-Limit 1000 Zeilen. Vorher aufteilen.
- Lieber `// TODO: <was fehlt> (Abschnitt N)` als halb durchdachte Methoden.
- Keine Fallback-Pfade, die Fehler verschleiern: Fehlt etwas, loggen und sauber abbrechen (Muster: `GameManager.ChangeScene`).
