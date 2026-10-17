---
paths:
  - "scenes/**"
  - "scripts/ui/**"
  - "project.godot"
---

# Godot-Regeln

- Klassen, die von Godot-Typen erben, sind `partial` (Source Generator). Dateiname = Klassenname.
- Node-Referenzen bevorzugt über `[Export]` setzen. `GetNode`-Pfade nur als Fallback, und das Fehlen immer loggen.
- Szenenwechsel ausschließlich über `GameManager.Instance.ChangeScene(path)`, nicht direkt über `GetTree().ChangeSceneToFile`.
- Signale in `_Ready` verbinden. Bei Nodes, die vor ihrem Sender freigegeben werden, in `_ExitTree` wieder trennen.
- `.tscn`, `.tres` und `project.godot` möglichst im Editor ändern. Bei Handbearbeitung keine `uid`/`unique_id` erfinden.
- `*.uid`-Dateien (neben `.cs`, `.gd`, `.gdshader`) gehören ins Repo, sonst brechen `uid://`-Verweise (z. B. Autoload `GameManager`).
- Haupttext später über `RichTextLabel` mit BBCode; Namen und Orte über `meta_clicked` anklickbar (Dokument Abschnitt 12).
- Spiel ist 2D: `Node2D`/`Control`, keine 3D-Nodes.
