# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Projekt

Tiefberg: 2D-Zivilisationssimulator mit RPG-Elementen (Textadventure mit GUI), Solo-Entwicklung. Godot 4.7.1 + C# auf .NET 10 (`net10.0`).

**Verbindliche Referenz:** `defines/Zivilisations-RPG – Design- & Architekturreferenz (KI-optimiert)(1).md`. Bei Widerspruch zwischen Code und Dokument gilt das Dokument. Vor Arbeit an einem Spielsystem (Zeit, Welt, Völker, Personen, RPG, Magie, Siedlung, Simulationsebenen, Speichern) den passenden Abschnitt dort lesen, statt Annahmen zu treffen. Status-Präfixe im Dokument:
- `E -` entschieden → umsetzen.
- `O -` offen → nicht erfinden, `// TODO:` mit Abschnittsverweis setzen (z. B. `// TODO: Abschnitt 8 – Regeneration offen`).
- `O - Vorschlag:` darf umgesetzt werden, im Code als Annahme markieren.

**Aufgaben:** `ABLAUFPLAN.md` ist die lebende Aufgabenliste. Vor einer Aufgabe dort nachsehen. Nach jeder erledigten Aufgabe aktualisieren: nach „Erledigt“ verschieben (Datum), Folgeaufgaben mit `← T-###` anlegen und die nächste freie ID hochzählen. Änderungen des Entwicklers an der Datei haben Vorrang und werden nicht zurückgedreht.

Detaillierte Regeln liegen in `.claude/rules/` (C#-Stil, Architektur, Godot) und werden je nach bearbeiteter Datei automatisch geladen.

## Befehle

```bash
dotnet build                 # kompiliert Tiefberg.csproj (Godot.NET.Sdk), schneller Compile-Check ohne Editor
```

- Spiel starten: im Godot-Editor (F5), Hauptszene ist `scenes/ui/MainMenu.tscn`.
- Tests und Linter gibt es noch nicht.
- Logs zur Laufzeit: `user://logs/game_*.log` (Windows: `%APPDATA%\Godot\app_userdata\Tiefberg\logs`), die letzten 10 werden behalten.

## Architektur

Ziel-Architektur (Dokument Abschnitt 2), noch größtenteils nicht umgesetzt:
- Schichten **Daten ← Simulation ← Szenario ← Präsentation**, Abhängigkeiten nur nach links.
- Simulationskern ist reines C# ohne `using Godot;`, tick-basiert, multicore-fähig. Godot liest nur Snapshots; die UI schickt Commands statt Methoden direkt aufzurufen.
- Inhalte (Spezies, Rezepte, Gebäude …) sind YAML-Daten, keine Klasse pro Eintrag.
- Zufall nur über seedbare RNG-Instanzen bzw. den zentralen Würfeldienst.
- Weltzeit: `ulong` Sekunden seit Weltstart; Kalender wird daraus berechnet.

Aktueller Stand (Meilenstein 1 „Fundament“ begonnen):
- `GameManager` (`scripts/core/`) ist Autoload-Singleton (`/root/GameManager`, `GameManager.Instance`). Initialisiert und beendet das Logging, bündelt Szenenwechsel (`ChangeScene`) und Start/Laden/Beenden.
- Logging: `LogManager` (statisch, Datei + Godot-Konsole, Thread-sicher, fängt unbehandelte Exceptions) und `GameLogger` pro Klasse über `LogManager.GetLogger<T>()`. Dieses Logging nutzt Godot-APIs und gehört damit zur Präsentations-/Host-Schicht. Für den Simulationskern ist laut Dokument `Microsoft.Extensions.Logging` oder Serilog mit Logger per Konstruktor und Weltzeit in jeder Meldung vorgesehen.
- `SaveService` ist nur ein Stub.

## Bekannte Abweichungen (Stand jetzt)

- `SaveService`-TODOs erwähnen JSON und `PetData`. Das sind Überbleibsel: Laut Dokument sind Spielstände binär und versioniert, mit getrenntem Welt- und Szenariozustand (T-012).
- `scenes/ui/Map.tscn` ist eine leere 3D-Szene, das Spiel ist 2D (T-014).
