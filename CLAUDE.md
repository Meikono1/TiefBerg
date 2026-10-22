# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Projekt

Tiefberg: 2D-Zivilisationssimulator mit RPG-Elementen (Textadventure mit GUI), Solo-Entwicklung. Godot 4.7.1 + C# auf .NET 10 (`net10.0`).

**Verbindliche Referenz:** `defines/ZivilisationArchitektur Design Referenz.md`. Bei Widerspruch zwischen Code und Dokument gilt das Dokument. Vor Arbeit an einem Spielsystem (Zeit, Welt, Völker, Personen, RPG, Magie, Siedlung, Simulationsebenen, Speichern) den passenden Abschnitt dort lesen, statt Annahmen zu treffen. Status-Präfixe im Dokument:
- `E -` entschieden → umsetzen.
- `O -` offen → nicht erfinden, `// TODO:` mit Abschnittsverweis setzen (z. B. `// TODO: Abschnitt 8 – Regeneration offen`).
- `O - Vorschlag:` darf umgesetzt werden, im Code als Annahme markieren.

**Keine Fremdnamen:** In Code, Kommentaren, Doku, Tickets und Daten keine Namen anderer Spiele, Firmen oder Fremdprodukte als Vorbild oder Vergleich nennen. Vorbilder beschreiben, was übernommen wird (z. B. „Szenarioliste links, Karte rechts“), nicht woher. Ausgenommen sind der eigene Technik-Stack (Godot, .NET, Bibliotheken) und Werkzeuge als Quellenangabe für erzeugte Daten (z. B. World Climate Lab in `lore/klima/`, `wind.yaml`).

**Keine Übergangslösungen:** Alles, was umgesetzt wird, ist so gebaut, als wäre es final. Keine Krücken „bis X fertig ist“, keine Ersatzwege für fehlende Zuweisungen oder Dateien, keine doppelten Pfade (mit/ohne). Fehlt eine Entscheidung oder ein Baustein, bleibt die Stelle bewusst leer: `// TODO:` mit Abschnitt bzw. Ticket und ein Eintrag in `ABLAUFPLAN.md`. Platzhalter sind nur als Inhalt erlaubt (Texte, Positionen, Bilder) und als solche markiert, nie als Code.

**Aufgaben:** `ABLAUFPLAN.md` ist die lebende Aufgabenliste. Vor einer Aufgabe dort nachsehen. Nach jeder erledigten Aufgabe aktualisieren: nach „Erledigt“ verschieben (Datum), Folgeaufgaben mit `← T-###` anlegen und die nächste freie ID hochzählen. Änderungen des Entwicklers an der Datei haben Vorrang und werden nicht zurückgedreht.

Detaillierte Regeln liegen in `.claude/rules/` (C#-Stil, Architektur, Godot) und werden je nach bearbeiteter Datei automatisch geladen.

## Befehle

```bash
dotnet build Tiefberg.sln              # baut Simulationskern, Tests + Godot-Projekt, schneller Compile-Check ohne Editor
dotnet test --solution Tiefberg.sln    # xUnit-Tests des Simulationskerns (tests/), ohne Godot
```

- Spiel starten: im Godot-Editor (F5), Hauptszene ist `scenes/ui/MainMenu.tscn`.
- Laufzeit-Check ohne Fenster (ca. 12 s, Log auf der Konsole; vorher `dotnet build Tiefberg.sln`):
  `& "C:\Users\Christian\Desktop\shadowrun\test\Godot_v4.7.1-stable_mono_win64_console.exe" --headless --path . --max-fps 120 --quit-after 1500 -- --new-game`
  Ohne `-- --new-game` bleibt das Spiel im Menü und die Simulation läuft nicht. `-- --continue` lädt den neuesten Spielstand. Beim Beenden entsteht ein Autosave.
- Spielstände: `user://saves/<slot>/world.sav` + `scenario.sav` (Windows: `%APPDATA%\Godot\app_userdata\Tiefberg\saves`).
- Tests: `tests/Tiefberg.Simulation.Tests` (xUnit v3 auf Microsoft.Testing.Platform, per `global.json`; daher `dotnet test --solution …` statt `dotnet test Tiefberg.sln`). Getestet wird nur der Godot-freie Kern; Logger dort `NullLogger<T>.Instance`. Linter gibt es noch nicht.
- Logs zur Laufzeit: `user://logs/game_*.log` (Windows: `%APPDATA%\Godot\app_userdata\Tiefberg\logs`), die letzten 10 werden behalten.

## Architektur

Ziel-Architektur (Dokument Abschnitt 2), noch größtenteils nicht umgesetzt:
- Schichten **Daten ← Simulation ← Szenario ← Präsentation**, Abhängigkeiten nur nach links.
- Simulationskern ist reines C# ohne `using Godot;`, tick-basiert, multicore-fähig. Godot liest nur Snapshots; die UI schickt Commands statt Methoden direkt aufzurufen.
- Inhalte (Spezies, Rezepte, Gebäude …) sind YAML-Daten, keine Klasse pro Eintrag.
- Zufall nur über seedbare RNG-Instanzen bzw. den zentralen Würfeldienst.
- Weltzeit: `ulong` Sekunden seit Weltstart; Kalender wird daraus berechnet.

Der Kern liegt als eigenes, Godot-freies Projekt `scripts/simulation/Tiefberg.Simulation.csproj` vor (Namespaces `TiefBerg.Data`, `TiefBerg.Simulation`, `TiefBerg.Scenario`); das Godot-Projekt referenziert es. Details: `.claude/rules/architecture.md`.

Aktueller Stand (Meilenstein 1 „Fundament“ begonnen):
- `GameManager` (`scripts/core/`) ist Autoload-Singleton (`/root/GameManager`, `GameManager.Instance`). Initialisiert und beendet das Logging, bündelt Szenenwechsel (`ChangeScene`) und Neues Spiel/Laden/Speichern/Beenden. Mit einem Spiel erzeugt bzw. lädt er eine `World` (Seed, Uhr, Scheduler, Würfel) und einen `ScenarioState`, startet den `SimulationRunner` und ruft vorerst pro Frame `Tick()` auf; der Tick schreibt die Weltzeit fort und arbeitet fällige Systeme über den `Scheduler` ab (`TiefBerg.Simulation.Scheduling`, Systeme implementieren `ISimulationSystem`; registriert ist noch keins).
- Weltzeit: `WorldClock` / `WorldCalendar` / `Calendar` in `TiefBerg.Simulation.Time`. 1 Tick = 1 Weltsekunde. Weltzeit 0 liegt weit in der Urzeit, die Simulation startet später (vorerst noch bei 0, T-025). Spezieskalender rechnen ab ihrer Epoche und kennen Daten davor („v. Z.“). Kern-Logzeilen zeigen die rohe Weltzeit als `[T=1-01-01 00:25:00]`.
- Logging: `LogManager` (statisch, Datei + Godot-Konsole, Thread-sicher, fängt unbehandelte Exceptions) und `GameLogger` pro Klasse über `LogManager.GetLogger<T>()`. Dieses Logging nutzt Godot-APIs und gehört damit zur Präsentations-/Host-Schicht. Der Simulationskern bekommt `ILogger<T>` (Microsoft.Extensions.Logging) per Konstruktor. `LogManager.SimulationLoggerFactory` erzeugt diese Logger, und `SimulationLoggerProvider` leitet sie in denselben `LogManager` um: eine Datei, ein Format, die Weltzeit aus `LogManager.WorldTimeSource` vorangestellt. Jede Zeile enthält Systemzeit und Klasse.Methode (`[CallerMemberName]`); im Kern ist das per Analyzer erzwungen.
- Zeitmessung: `TiefBerg.Diagnostics.Perf` misst pro Klasse.Methode, warnt bei Budgetüberschreitung (`PerfBudgets.TickMs = 8` ms für 120 FPS), und `GameManager._Process` loggt alle 10 s eine Aufwandsübersicht.
- Zufall: `DiceService` / `RandomSource` in `TiefBerg.Simulation.Dice` (xoshiro256**, Zustand speicherbar), über `World.Dice`. `System.Random` ist im Kern gesperrt.
- Speichern: Format im Kern (`WorldStateSerializer`, `ScenarioStateSerializer`, `SaveFormat`), Dateien und Autosave-Rotation im `SaveService` (Godot). Simulationssysteme brauchen eine feste `Id` (Zuordnung im Spielstand).

## Bekannte Abweichungen (Stand jetzt)

- `scenes/ui/Map.tscn` ist eine leere 3D-Szene, das Spiel ist 2D (T-014).
