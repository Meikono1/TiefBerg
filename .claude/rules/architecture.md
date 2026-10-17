---
paths:
  - "scripts/**"
  - "data/**"
---

# Architekturregeln

Quelle: Design-Dokument Abschnitte 2, 3, 7, 10, 12. Bei Unklarheit dort nachlesen.

## Schichten
Daten ← Simulation ← Szenario ← Präsentation. Abhängigkeiten nur nach links.
- **Simulation/Daten/Szenario:** reines C#, kein `using Godot;`, keine Godot-Typen (`Vector2`, `Node`, `GD.*`, `FileAccess`). Damit bleibt der Kern testbar und multicore-fähig.
- **Präsentation (Godot):** liest nur Snapshots des Simulationszustands und schickt Commands (Command-Pattern), statt Simulationsmethoden direkt aufzurufen.
- Der Weltzustand kennt das Szenario nicht. Szenariozustand (z. B. Tutorial-Fortschritt) wird getrennt gehalten und gespeichert.

## Determinismus
- Zufall ausschließlich über übergebene, seedbare RNG-Instanzen bzw. den zentralen Würfeldienst. Kein `new Random()` ohne Seed, kein `Random.Shared`, kein `GD.Randf()` im Kern.
- Gleicher Seed → gleiche Welt. Iteration über `Dictionary`/`HashSet` nicht für ergebnisrelevante Reihenfolgen nutzen.
- Jeder Würfelwurf wird auf Debug geloggt.

## Zeit und Scheduling
- Weltzeit ist ein `ulong` (Sekunden seit Weltstart). Kalender (60 s, 60 min, 20 h/Tag, 23 Tage/Monat, 12 Monate) nur für die Anzeige daraus berechnen.
- Systeme laufen in eigenen Takten über eine nach Fälligkeit sortierte Liste, nicht durch Polling jedes Ticks.

## Daten
- Inhalte (Spezies, Kulturen, Rezepte, Gebäude …) als YAML unter `data/`, keine Klasse pro Eintrag (Modding).
- Massendaten (statistische Bevölkerung) in Arrays/Structs statt tiefer Objektgraphen.
- Personen werden per ID referenziert (IDs nie wiederverwenden); Eltern, Kinder und Partner sind gegenseitig verlinkt.

## Speichern
- Binär, versioniert, mit Seed und Weltzeit. Welt- und Szenariozustand getrennt.
- Autosaves rotieren und überschreiben keine älteren Stände.
