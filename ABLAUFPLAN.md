# Ablaufplan

Lebende Aufgabenliste. Claude aktualisiert sie nach jeder erledigten Aufgabe, du darfst jederzeit frei korrigieren, umsortieren oder streichen.

**Regeln**
- Jede Aufgabe hat eine feste ID (`T-###`, nie wiederverwendet). Neue Aufgaben bekommen die nächste freie Nummer.
- Ergibt sich aus einer Aufgabe eine neue, steht bei der neuen `← T-###` (Herkunft).
- Erledigte Aufgaben wandern nach **Erledigt**, mit Datum und ggf. `→ T-###` (Folgeaufgaben).
- Verweise auf das Design-Dokument als `(Abschnitt N)`.
- Nächste freie ID: **T-041**

## In Arbeit

_(nichts)_

## Als Nächstes – Szenarioauswahl (Abschnitt 11, 12)

Ablauf: Hauptmenü → Start → Szenarioauswahl (Layout nach EU4, Bauplan `defines/UI Szenarioauswahl.md`) → Szenario wählen → Weiter bzw. Doppelklick.

- [ ] **T-031** Szenario-Konfigurationsdatei: YAML unter `data/scenarios/` mit Id, Übersetzungsschlüssel für Titel/Beschreibung, Position auf der Weltkarte (Pixel der Originalkarte, damit unabhängig von Skalierung), Icon (aus `resources/MapIcons/`), Pfad zum Vorschaubild, Tags (Spezies, Region, Schwierigkeit), Startjahr (bis T-025 als Anzeigewert), gesperrt ja/nein + Freischalt-Hinweis, Neuigkeiten (Datum, Version, Art neu/geändert/geplant, Textschlüssel für Änderungen und Features); später Startzeit (T-025) und Startort. Ladecode im Kern (`TiefBerg.Data`), ungültige Einträge loggen und überspringen, Tests. YAML-Bibliothek auswählen (erste YAML-Daten im Projekt) ← T-030.
- [ ] **T-032** Weltkarte als Spielressource: `lore/weltkarte.png` (4320×2160, 5,8 MB) als Ressource übernehmen, Import-Einstellungen prüfen (Mipmaps an und `WorldMap` mit Filter „Linear Mipmap“ seit 2026-10-20, Kompression noch offen; liegt noch unter `lore/`). Map-Icons (`resources/MapIcons/`, 256×256, schwarz mit Alpha) importieren; Zustand über Ring dahinter statt Einfärben (Abschnitt 12, Vorschlag). Platzhalter-Vorschaubild für Szenarien ohne eigenes Bild ← T-030.
- [ ] **T-036** (Entwickler) Szenen und Theme im Godot-Editor bauen nach `defines/UI Szenarioauswahl.md`: Projekt-Grundeinstellungen (1920×1080, `canvas_items`, `expand`), `TiefbergTheme.tres` mit Typ-Varianten, `ScenarioSelect.tscn`, `scenario/ScenarioMarker.tscn`, `scenario/ScenarioListEntry.tscn`, `scenario/NewsCard.tscn`, `scenario/MapModeButton.tscn`, `ScenarioButtonGroup.tres`, `MapModeButtonGroup.tres`. Node-Namen und -Typen wie im Bauplan, Optik frei ← T-030.
- [ ] **T-033** Scripts `ScenarioSelect.cs`, `MapPanZoom.cs`, `ScenarioMarker.cs`, `ScenarioListEntry.cs`, `NewsCard.cs`, `MapModeButton.cs` unter `scripts/ui/scenario/` nach Bauplan (Exports, Signale, Verhalten Abschnitt 11 dort). `MapPanZoom` ist umgesetzt (2026-10-18: Mausrad-Zoom zur Maus, Ziehen links/mitte, Buttons, Zurücksetzen, Grenzen, Fokus-Tween, Texturwechsel, `SourceToView`; Build grün, im Editor vom Entwickler geprüft). Übrige Gerüste liegen seit 2026-10-17 vor (Namespace `TiefBerg.UI.Scenario`, Exports in Gruppen, Signale, Prüfung fehlender Exports über `TiefBerg.UI.ExportCheck`, Verhalten als `TODO: T-033`/`T-037`); `.cs.uid` erzeugt der Editor beim ersten Öffnen. Inhalt (Abschnitt 12): Karte mit Pan (Ziehen) und Zoom (Mausrad, Buttons, Zurücksetzen), Punkte mit Icon aus der Konfiguration, Szenarioliste und Details links, Jahresanzeige und Kartenmodi oben, neueste Szenarien unten, „Zoom zum Punkt“ bei Auswahl (abschaltbar), gesperrte Szenarien nicht startbar, Doppelklick oder Weiter startet, Zurück ins Hauptmenü, Tastatur (Pfeil hoch/runter, Enter, Esc), Tooltip am Punkt. `ScenePaths` ergänzen ← T-030, T-031, T-032, T-036.
- [ ] **T-037** Kartenmodi für die Szenarioauswahl: Konfiguration (Id, Namensschlüssel, Icon, Textur), Start mit Terrain (`weltkarte.png`), Klima (`02-climate_zones-raw.png`), Vegetation (`03-vegetation-raw.png`), Völker (`voelkerkarte.png`). Größen prüfen und angleichen: `voelkerkarte.png` ist 4320×2560 statt 4320×2160 (Ausrichtung klären). Import wie T-032 (Abschnitt 4, Liste der Bitmap-Layer ist offen) ← T-030.
- [ ] **T-034** Ablauf verdrahten: Start im Hauptmenü öffnet die Szenarioauswahl (TODO in `MainMenu.OnStartPressed`), `GameManager.StartNewGame(scenarioId)` übernimmt die gewählte Szenario-Id in den `ScenarioState` (und in den Spielstand). `-- --new-game` ohne Id startet Szenario 1 ← T-033.
- [ ] **T-035** Texte über Schlüssel: Grundgerüst Übersetzung (Godot `TranslationServer`, Englisch als einzige Sprache), zuerst für Hauptmenü und Szenarioauswahl (Abschnitt 12) ← T-031.

## Als Nächstes – Meilenstein 1: Fundament (Abschnitt 13)

- [ ] **T-027** Simulations-Tick vom Godot-Frame entkoppeln: eigener Takt bzw. Thread, Godot liest nur Snapshots (Abschnitt 2). Heute 1 Tick pro Frame, d. h. Spielgeschwindigkeit hängt an der FPS ← T-010.
- [ ] **T-022** Zeit in Tagen, Monaten und Jahren laufen lassen (Zeitraffer, auch „bis zum nächsten Ereignis“). Partien bis 500+ Jahre (≈ 10^10 Ticks), daher Sprung von Fälligkeit zu Fälligkeit statt Sekunde für Sekunde. Braucht den Scheduler (Abschnitt 3) ← T-009, T-010.
- [ ] **T-023** Spezieskalender (`Calendar`: Id + Epoche) aus den Speziesdaten laden, Monatsnamen über Übersetzungsschlüssel (Abschnitt 3, 5, 12) ← T-009, T-020.
- [ ] **T-025** Startzeit der Simulation aus Welt-/Szenariodaten statt Weltzeit 0 (`WorldClock`-Start im `GameManager`); konkreter Wert und Abstand zu Weltzeit 0 noch offen (Abschnitt 3, 11) ← T-024.

## Danach / Ideen

- [ ] **T-039** Seewind: Windrichtung neu erzeugen. Die heutigen `wind_mXX.bin` leiten die Richtung aus dem Wandern der Starkwindflecken in der WCL-Animation ab; Prüfung am 2026-10-20: zwischen 0° und 30° wehen 88 % der Wasserzellen nach Osten (Passate fehlen), keine Windgürtel erkennbar, das erzeugende Skript („flow“) liegt nicht im Projekt. Vorschlag: reproduzierbarer Generator in `tools/` (Windgürtel nach Breitengrad, Jet-Breiten aus `wind.yaml`, Stärke aus WCL), Format bleibt. Vorher klären: Drehrichtung des Planeten (`Klima-und-Handel.md`, offen) (Abschnitt 4, 9) ← T-038.
- [ ] **T-040** `tools/seeroute.py`: Beim Rösselsprung (2,1) werden die Zwischenzellen (i+1, j) und (i, j+1) geprüft statt (i+1, j) und (i+1, j+1); Routen können dadurch an Landzipfeln abkürzen ← T-038.

- [ ] **T-014** `scenes/ui/Map.tscn` ist eine leere 3D-Szene → durch 2D-Szene ersetzen (Spiel ist 2D).
- [ ] **T-018** Frame-Überwachung auf Godot-Seite: Frames über 8,33 ms (unter 120 FPS) zählen bzw. in die Aufwandsübersicht aufnehmen ← T-017.
- [ ] **T-029** Manuelle Spielstände: Bricht das Spiel genau zwischen „alter Stand → `.old`“ und „`.tmp` → Stand“ ab, liegt der Stand nur noch als `<slot>.old`/`<slot>.tmp` vor und wird nicht gefunden. Beim Start erkennen und wiederherstellen (Autosaves sind nicht betroffen, sie bekommen immer neue Ordner) ← T-012.
- [ ] **T-026** Vor dem ersten Export prüfen: In `Tiefberg.sln` sind `ExportDebug`/`ExportRelease` für `Tiefberg.Simulation` auf `Debug` gemappt. Klären, ob Godot beim Export dieses Mapping nutzt; falls ja, Kern im Release-Export optimiert bauen (`Release`).
- [ ] **T-016** Hauptmenü: Fortsetzen-Button bei vorhandenem Spielstand, Optionen (Auflösung, Sprache, Log-Level) ← T-012.

- [ ] **T-020** Rassen-Daten umbauen: `lore/Races V3.doc` ist eine Zusammenfassung mit mehr Infos als nötig und erst ~40 % fertig. Auf spielrelevante Werte reduzieren (Größenklasse, Lebensdauer, Erwachsenenalter, Ernährung, Präferenzen, Reproduktionstabelle) und als YAML unter `data/` anlegen. Inhalt liefert der Entwickler (Abschnitt 5).

Meilenstein 2 (Prototyp Expedition) wird aufgeschlüsselt, wenn Meilenstein 1 steht.

## Erledigt

- [x] **T-038** 2026-10-20 `data/world` aufgeräumt: WCL-Rohmaterial (`all-layers`, `climate-data`, Wind- und Meeresströmungs-Aufnahmen mit GIFs) nach `lore/world/` mit `.gdignore` verschoben, nichts gelöscht; `.import`-Dateien und 698 verwaiste Cache-Dateien entfernt (`.godot/imported` 340 → 66 MB). In `data/world` bleiben `sea/navgrid.bin` und `wind/` (28 MB). `.bin`-Format geprüft und korrekt, Windrichtung fraglich. Hinweis: Die Ebenen im Wurzelordner von `lore/` sind identische Kopien aus `lore/world/all-layers` → T-039, T-040
- [x] **T-030** 2026-10-17 UI der Szenarioauswahl entworfen (Mockup im Chat): Karte mit Pan/Zoom, Punkte mit Icons aus `resources/MapIcons/`, Szenario-Leiste, Panel fest rechts, „Zoom zum Punkt“ abschaltbar, gesperrte Szenarien sichtbar, Start per Doppelklick oder Button. Danach auf Layout nach EU4 umgestellt (Szenarioliste und Details links, Jahr und Kartenmodi oben, neueste Szenarien unten, Zurück unten links, Weiter unten rechts). Entscheidungen in Abschnitt 12, Godot-Bauplan in `defines/UI Szenarioauswahl.md` → T-031, T-032, T-033, T-034, T-036, T-037
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
- [x] **T-015** 2026-10-17 Testprojekt `tests/Tiefberg.Simulation.Tests` (xUnit v3 über Microsoft.Testing.Platform, `global.json`), in der `.sln`, aus dem Godot-Build ausgeschlossen, `.gdignore`. Erste Tests für `SimulationRunner`, alle grün ← T-007
- [x] **T-009** 2026-10-17 Weltzeit in `TiefBerg.Simulation.Time`: `WorldClock` (`ulong`, Volatile, Überlauf = Error), `WorldCalendar` (Kalenderaufbau, Umrechnung beide Richtungen, Format `1-01-01 00:00:00`, Jahr/Monat/Tag ab 1 als Annahme), `Calendar` je Spezies mit Epoche, `CalendarDate`. Tick = 1 s fest. Design-Dokument Abschnitt 3 ergänzt (Tick, Kampfrunde 6 s, Spezieskalender, 500 Jahre). `SimulationRunner` schreibt die Uhr pro Tick fort, `GameManager` setzt `LogManager.WorldTimeSource`, Logzeilen zeigen `[T=Datum]`. 35 Tests grün, Headless-Lauf geprüft ← T-008 → T-022, T-023, T-024
- [x] **T-012** 2026-10-17 Speichern und Laden. Kern: `World` bündelt Seed, `WorldClock`, `Scheduler`, `DiceService` (`Create`/`Restore`/`CaptureState`); `WorldStateSerializer` und `ScenarioStateSerializer` (binär, Kennung + Version + Endmarke, `SaveFormat` in `TiefBerg.Data.Persistence`, defekte/abgeschnittene/neuere Dateien → `SaveFormatException`). Systeme haben eine feste `Id`, über die Fälligkeiten zugeordnet werden. Godot: `SaveService` mit Ordner je Spielstand (`world.sav`, `scenario.sav`), Schreiben über `.tmp`, Autosaves mit Zeitstempel, die 5 neuesten bleiben. `GameManager`: `StartNewGame` (Seed zufällig), `LoadGame` (neuester Stand), `SaveGame(slot)`, Kommandozeile `-- --new-game` / `-- --continue`. 103 Tests grün; headless Neues Spiel → Autosave → Fortsetzen (Weltzeit läuft weiter) und Rotation geprüft ← T-011 → T-029
- [x] **T-013** 2026-10-17 Autosave in `GameManager._ExitTree`, wenn ein Spiel läuft ← T-012
- [x] **T-028** 2026-10-17 In T-012 aufgegangen: `World.Create` erzeugt den `DiceService` aus dem Weltseed ← T-011
- [x] **T-011** 2026-10-17 Würfeldienst in `TiefBerg.Simulation.Dice`: `RandomSource` (eigenes xoshiro256** mit SplitMix64-Seeding, Zustand speicherbar, `Derive(seed, key)` für deterministische Teilströme z. B. pro Ort), `DiceService` (W*n*, *m*W*n*, Probe W20 + Level + Mod ≥ SG, Level 0–30, `SuccessChance` für die Statistik-Ebene), jeder Wurf auf Debug mit Aufrufer. `System.Random` im Kern per Analyzer gesperrt. SG-Formel als TODO (Abschnitt 7 offen). Nachgezogen: vier Erfolgsgrade (±10), Rettungswurf-Schaden (0 / halb / voll / doppelt), `Chances` je Grad; natürliche 20/1 offen. 77 Tests grün ← T-001 → T-028
- [x] **T-010** 2026-10-17 `Scheduler` in `TiefBerg.Simulation.Scheduling`: `PriorityQueue` nach (Fälligkeit, laufende Nummer), deterministisch bei gleicher Fälligkeit; Takt zählt ab alter Fälligkeit (kein Drift), holt nach Sprüngen jeden Lauf nach; `NextDue` für T-022. Systeme über `ISimulationSystem` (`IntervalSeconds`, `Run(due)`), jedes einzeln per `Perf.MeasureAs` ohne Budget gemessen. `SimulationRunner.Tick` ruft `RunDue`. Fehler im System → Error-Log und Abbruch. 55 Tests grün, Headless-Lauf geprüft ← T-017 → T-027
- [x] **T-024** 2026-10-17 Ereignisse vor dem Simulationsstart: Weltzeit 0 liegt weit in der Urzeit, die Simulation startet später (`ulong` bleibt). `Calendar` hat eine Epoche statt Offset und kann Daten vor Jahr 1 (`CalendarDate.BeforeEra`, „v. Z.“, kein Jahr 0). Design-Dokument Abschnitt 3 angepasst, 47 Tests grün ← T-009 → T-025
- [x] **T-021** 2026-10-17 Design-Doku umbenannt (`defines/ZivilisationArchitektur Design Referenz.md`), Rohnotizen entfernt, 8-ms-Tick und Logformat als `E -` ergänzt; Nullable im Kern aktiv (Verstöße = Build-Fehler), Build und Headless-Lauf grün
