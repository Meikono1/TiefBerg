---
paths:
  - "scenes/**"
  - "scripts/ui/**"
  - "project.godot"
---

# Godot-Regeln

- Klassen, die von Godot-Typen erben, sind `partial` (Source Generator). Dateiname = Klassenname.
- Node- und Szenen-Referenzen ausschließlich über `[Export]`, im Inspector zugewiesen. Keine `GetNode`-Pfade und keine Ersatzpfade für fehlende Zuweisungen: `_Ready` prüft alle Exports mit `ExportCheck.AllAssigned`, loggt Fehlendes als Error und bricht ab.
- Module (zur Laufzeit instanzierte Szenen) haben ein eigenes Script mit `Setup(...)`; Daten nicht über Kind-Suche (`FindChild`) in fremde Szenen schreiben.
- Szenenwechsel ausschließlich über `GameManager.Instance.ChangeScene(path)`, nicht direkt über `GetTree().ChangeSceneToFile`.
- Signale in `_Ready` verbinden. Bei Nodes, die vor ihrem Sender freigegeben werden, in `_ExitTree` wieder trennen.
- `.tscn`, `.tres` und `project.godot` möglichst im Editor ändern. Bei Handbearbeitung keine `uid`/`unique_id` erfinden.
- `*.uid`-Dateien (neben `.cs`, `.gd`, `.gdshader`) gehören ins Repo, sonst brechen `uid://`-Verweise (z. B. Autoload `GameManager`). Erzeugt Godot sie neu, bekommen sie neue UIDs. Dann die Verweise in `project.godot` und `.tscn` mit dem Inhalt der `.uid`-Dateien abgleichen (Symptom: „Unrecognized UID“ / „Failed to instantiate an autoload“).
- Haupttext später über `RichTextLabel` mit BBCode; Namen und Orte über `meta_clicked` anklickbar (Dokument Abschnitt 12).
- Spiel ist 2D: `Node2D`/`Control`, keine 3D-Nodes.
