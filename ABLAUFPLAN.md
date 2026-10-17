# Ablaufplan

Lebende Aufgabenliste. Claude aktualisiert sie nach jeder erledigten Aufgabe, du darfst jederzeit frei korrigieren, umsortieren oder streichen.

**Regeln**
- Jede Aufgabe hat eine feste ID (`T-###`, nie wiederverwendet). Neue Aufgaben bekommen die nächste freie Nummer.
- Ergibt sich aus einer Aufgabe eine neue, steht bei der neuen `← T-###` (Herkunft).
- Erledigte Aufgaben wandern nach **Erledigt**, mit Datum und ggf. `→ T-###` (Folgeaufgaben).
- Verweise auf das Design-Dokument als `(Abschnitt N)`.
- Nächste freie ID: **T-017**

## In Arbeit

_(nichts)_

## Als Nächstes – Meilenstein 1: Fundament (Abschnitt 13)

- [ ] **T-007** Ordner- und Namespace-Struktur für den Simulationskern festlegen (reines C#, ohne Godot), z. B. `scripts/simulation/` → `TiefBerg.Simulation` (Abschnitt 2).
- [ ] **T-008** Logging für den Simulationskern: `Microsoft.Extensions.Logging` oder Serilog wählen, Logger per Konstruktor, Weltzeit in jeder Meldung. Anbindung an den bestehenden `LogManager` klären (Abschnitt 12).
- [ ] **T-009** Weltzeit: `ulong`-Zähler plus Kalenderumrechnung (60 s, 60 min, 20 h, 23 Tage, 12 Monate) (Abschnitt 3).
- [ ] **T-010** Scheduler: nach Fälligkeit sortierte Liste, Systeme mit eigenem Takt (Abschnitt 3).
- [ ] **T-011** Zentraler, seedbarer Würfeldienst, jeder Wurf auf Debug geloggt (Abschnitt 7).
- [ ] **T-012** Speichern und Laden: binär, versioniert, Seed und Weltzeit, Welt- und Szenariozustand getrennt, rotierende Autosaves. Ersetzt die veralteten JSON/`PetData`-TODOs in `SaveService` (Abschnitt 12).
- [ ] **T-013** `GameManager`: Autosave in `_ExitTree` anbinden, sobald T-012 steht ← T-012.

## Danach / Ideen

- [ ] **T-014** `scenes/ui/Map.tscn` ist eine leere 3D-Szene → durch 2D-Szene ersetzen (Spiel ist 2D).
- [ ] **T-015** Vorschlag: Testprojekt (z. B. xUnit) für den Godot-freien Simulationskern, sobald T-007 steht ← T-007.
- [ ] **T-016** Hauptmenü: Fortsetzen-Button bei vorhandenem Spielstand, Optionen (Auflösung, Sprache, Log-Level) ← T-012.

Meilenstein 2 (Prototyp Expedition) wird aufgeschlüsselt, wenn Meilenstein 1 steht.

## Erledigt

- [x] **T-001** Repo und Godot-Projekt aufgesetzt.
- [x] **T-002** Logging für Godot-Seite (`LogManager`, `GameLogger`).
- [x] **T-003** 2026-10-17 CLAUDE.md, `.claude/rules/`, `.editorconfig` angelegt → T-004
- [x] **T-004** 2026-10-17 `MainMenu.cs` repariert, `ScenePaths` eingeführt, Build grün ← T-003 → T-005
- [x] **T-005** 2026-10-17 `.cs.uid`-Dateien im Editor erzeugt ← T-004
- [x] **T-006** 2026-10-17 csproj auf `net10.0` umgestellt (Runtime 10.0.11 laut Abschnitt 2), Android-Sonderzeile `net9.0` entfernt, Build grün
