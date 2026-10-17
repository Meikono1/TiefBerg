# Ablaufplan

Lebende Aufgabenliste. Claude aktualisiert sie nach jeder erledigten Aufgabe, du darfst jederzeit frei korrigieren, umsortieren oder streichen.

**Regeln**
- Jede Aufgabe hat eine feste ID (`T-###`, nie wiederverwendet). Neue Aufgaben bekommen die nächste freie Nummer.
- Ergibt sich aus einer Aufgabe eine neue, steht bei der neuen `← T-###` (Herkunft).
- Erledigte Aufgaben wandern nach **Erledigt**, mit Datum und ggf. `→ T-###` (Folgeaufgaben).
- Verweise auf das Design-Dokument als `(Abschnitt N)`.
- Nächste freie ID: **T-022**

## In Arbeit

_(nichts)_

## Als Nächstes – Meilenstein 1: Fundament (Abschnitt 13)

- [ ] **T-009** Weltzeit in `TiefBerg.Simulation`: `ulong`-Zähler plus Kalenderumrechnung (60 s, 60 min, 20 h, 23 Tage, 12 Monate) (Abschnitt 3). Danach `LogManager.WorldTimeSource` setzen und die Weltzeit im `SimulationLoggerProvider` als Datum formatieren ← T-008.
- [ ] **T-010** Scheduler in `TiefBerg.Simulation`: nach Fälligkeit sortierte Liste, Systeme mit eigenem Takt (Abschnitt 3). Tick mit `Perf.Measure(PerfBudgets.TickMs)` messen, jedes System einzeln ohne Budget ← T-017.
- [ ] **T-011** Zentraler, seedbarer Würfeldienst in `TiefBerg.Simulation`, jeder Wurf auf Debug geloggt (Abschnitt 7).
- [ ] **T-012** Speichern und Laden: binär, versioniert, Seed und Weltzeit, Welt- und Szenariozustand getrennt, rotierende Autosaves. Ersetzt die veralteten JSON/`PetData`-TODOs in `SaveService` (Abschnitt 12).
- [ ] **T-013** `GameManager`: Autosave in `_ExitTree` anbinden, sobald T-012 steht ← T-012.

## Danach / Ideen

- [ ] **T-014** `scenes/ui/Map.tscn` ist eine leere 3D-Szene → durch 2D-Szene ersetzen (Spiel ist 2D).
- [ ] **T-015** Vorschlag: Testprojekt (z. B. xUnit) für `Tiefberg.Simulation.csproj`. Jetzt möglich, weil der Kern kein Godot braucht ← T-007.
- [ ] **T-018** Frame-Überwachung auf Godot-Seite: Frames über 8,33 ms (unter 120 FPS) zählen bzw. in die Aufwandsübersicht aufnehmen ← T-017.
- [ ] **T-016** Hauptmenü: Fortsetzen-Button bei vorhandenem Spielstand, Optionen (Auflösung, Sprache, Log-Level) ← T-012.

- [ ] **T-020** Rassen-Daten umbauen: `lore/Races V3.doc` ist eine Zusammenfassung mit mehr Infos als nötig und erst ~40 % fertig. Auf spielrelevante Werte reduzieren (Größenklasse, Lebensdauer, Erwachsenenalter, Ernährung, Präferenzen, Reproduktionstabelle) und als YAML unter `data/` anlegen. Inhalt liefert der Entwickler (Abschnitt 5).

Meilenstein 2 (Prototyp Expedition) wird aufgeschlüsselt, wenn Meilenstein 1 steht.

## Erledigt

- [x] **T-001** Repo und Godot-Projekt aufgesetzt.
- [x] **T-002** Logging für Godot-Seite (`LogManager`, `GameLogger`).
- [x] **T-003** 2026-10-17 CLAUDE.md, `.claude/rules/`, `.editorconfig` angelegt → T-004
- [x] **T-004** 2026-10-17 `MainMenu.cs` repariert, `ScenePaths` eingeführt, Build grün ← T-003 → T-005
- [x] **T-005** 2026-10-17 `.cs.uid`-Dateien im Editor erzeugt ← T-004
- [x] **T-006** 2026-10-17 csproj auf `net10.0` umgestellt (Runtime 10.0.11 laut Abschnitt 2), Android-Sonderzeile `net9.0` entfernt, Build grün
- [x] **T-007** 2026-10-17 Simulationskern als eigenes, Godot-freies Projekt `scripts/simulation/Tiefberg.Simulation.csproj` (Namespaces `TiefBerg.Data`, `.Simulation`, `.Scenario`), im Godot-Projekt referenziert, in der `.sln`, Build grün → T-008, T-015
- [x] **T-008** 2026-10-17 Kern-Logging: `ILogger<T>` (M.E.Logging.Abstractions 10.0.11) per Konstruktor; `SimulationLoggerProvider` leitet in den `LogManager` um, inkl. Weltzeit-Präfix. Serilog bleibt als Backend später ohne Kernänderung möglich, Build grün ← T-007 → T-009
- [x] **T-017** 2026-10-17 Jede Logzeile mit Systemzeit und Klasse.Methode (`[CallerMemberName]`), im Kern per BannedApiAnalyzer erzwungen; `Perf.Measure()` mit Budget-Warnung (Tick 8 ms / 120 FPS) und Aufwandsübersicht alle 10 s. Sperre und Perf per Testlauf geprüft ← T-008 → T-010, T-018
- [x] **T-019** 2026-10-17 `SimulationRunner` (Start/Tick/Stop, Tick mit `Perf.Measure(PerfBudgets.TickMs)`), vom `GameManager` ab Spielstart pro Frame getickt. Veraltete UIDs in `project.godot`/`MainMenu.tscn` korrigiert (Autoload lud vorher gar nicht). Headless geprüft: Übersicht kommt alle 10 s ← T-017 → T-010
- [x] **T-021** 2026-10-17 Design-Doku umbenannt (`defines/ZivilisationArchitektur Design Referenz.md`), Rohnotizen entfernt, 8-ms-Tick und Logformat als `E -` ergänzt; Nullable im Kern aktiv (Verstöße = Build-Fehler), Build und Headless-Lauf grün
