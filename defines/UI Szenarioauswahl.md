# UI Szenarioauswahl – Bauplan für Godot

Bauplan für die Szenen der Szenarioauswahl (T-030 ff.). Die Szenen baut der Entwickler im Editor, die Scripts kommen von Claude. Damit beides zusammenpasst, sind Node-**Namen** und **Typen** verbindlich. Optik (Farben, Abstände, Schriften, StyleBoxes) ist frei und läuft über das Theme.

**Vorlage: Europa Universalis IV** (Startbildschirm/Lesezeichen-Auswahl). Entscheidungen dazu stehen in `ZivilisationArchitektur Design Referenz.md`, Abschnitt 12.

```
┌──────────────────────────────────────────────────────────────────────┐
│ [Startjahr 312]                    [Zoom zum Punkt]  Kartenmodus ▣▣▣▣ │  TopBar
├───────────────┬──────────────────────────────────────────────────────┤
│ Szenarien     │                                                      │
│ ▸ Expedition  │                                                      │
│   Naga 🔒     │                  Weltkarte (Pan/Zoom)                │
├───────────────┤                  Punkte mit Icons                    │
│ Bild          │                                                      │
│ Titel, Tags   │                                              [+][−][◎]│
│ Beschreibung  │                                                      │
├───────────────┴──────────────────────────────────────────────────────┤
│ [Zurück]   [Neu: Expedition – v0.1 …] [Geplant: Naga – …]  [Weiter →]│  BottomBar
└──────────────────────────────────────────────────────────────────────┘
```

## 1. Inspiration aus anderen Spielen

| Spiel | Was dort gut funktioniert | Übernahme |
|---|---|---|
| **Europa Universalis IV** | Datum oben, Kartenmodi oben rechts, Details links, Lesezeichen unten, Zurück unten links, Spielen unten rechts; Karte bleibt immer bedienbar | Grundlayout |
| Crusader Kings III | Auswahl zoomt die Karte an die Region, Charaktere als Punkte auf der Karte | Zoom zum Punkt, Punkte mit Icon |
| RimWorld | Szenarien als Daten, Beschreibung mit Platz für längeren Text | Konfigurationsdatei, scrollbare Beschreibung |
| Battle Brothers | Bild, Kurztext, Schwierigkeit; gesperrte zeigen, wie man sie freischaltet | Tags im Detailbereich, Freischalt-Hinweis |
| Kenshi | Sehr knappe Startliste, sofort spielbar | Weiter per Enter, keine Zwischendialoge |

Daraus übernommen (als Vorschlag):
- Tooltip beim Hover über einen Kartenpunkt (Name + Schwierigkeit).
- Tastatur: Pfeil hoch/runter blättert durch die Szenarioliste, Enter = Weiter, Esc = Zurück. Wichtig für Gamepad später.
- Letzte Auswahl merken und beim nächsten Öffnen vorauswählen.
- Zoom-Animation kurz halten (≈ 0,4 s, Ease-Out).
- Doppelklick bleibt als Abkürzung, Hauptweg ist Auswählen + Weiter.

## 2. Projekt-Grundeinstellungen (einmalig, Project Settings)

| Einstellung | Wert | Warum |
|---|---|---|
| `display/window/size/viewport_width` / `_height` | 1920 / 1080 | Bezugsgröße für alle UI-Maße |
| `display/window/stretch/mode` | `canvas_items` | UI skaliert sauber mit der Fenstergröße |
| `display/window/stretch/aspect` | `expand` | Breitere Bildschirme zeigen mehr Karte statt schwarzer Ränder |
| `gui/theme/custom` | `res://resources/ui/TiefbergTheme.tres` | Ein zentrales Theme für alle Menüs |

## 3. Dateien

```
scenes/ui/ScenarioSelect.tscn                Hauptszene
scenes/ui/scenario/ScenarioMarker.tscn       Punkt auf der Karte          (pro Szenario instanziert)
scenes/ui/scenario/ScenarioListEntry.tscn    Eintrag in der Liste links   (pro Szenario instanziert)
scenes/ui/scenario/NewsCard.tscn             Karte „neueste Szenarien“ unten (pro Eintrag instanziert)
scenes/ui/scenario/MapModeButton.tscn        Button für einen Kartenmodus (pro Modus instanziert)
resources/ui/TiefbergTheme.tres              Theme
resources/ui/ScenarioButtonGroup.tres        ButtonGroup für die Szenarioliste
resources/ui/MapModeButtonGroup.tres         ButtonGroup für die Kartenmodi
resources/MapIcons/*.png                     Icons der Punkte (vorhanden)
scripts/ui/scenario/ScenarioSelect.cs        (Claude)
scripts/ui/scenario/MapPanZoom.cs            (Claude) Pan/Zoom und Kartenmodus auf MapView
scripts/ui/scenario/ScenarioMarker.cs        (Claude)
scripts/ui/scenario/ScenarioListEntry.cs     (Claude)
scripts/ui/scenario/NewsCard.cs              (Claude)
scripts/ui/scenario/MapModeButton.cs         (Claude)
```

Scripts kannst du schon vorab anhängen, sobald die leeren Klassen existieren. Sonst hängst du sie nach T-033 an.

## 4. Hauptszene `ScenarioSelect.tscn`

Die Karte besteht nur aus `Control`-Nodes, ohne SubViewport und Camera2D. So funktionieren Klicks, Hover, Tooltips und Doppelklick wie bei normaler UI, und im Editor ist alles sichtbar. Pan und Zoom verschieben bzw. skalieren nur `MapContent`. Ein Kartenmodus tauscht die Textur von `WorldMap` aus.

```
ScenarioSelect                     Control          Full Rect                                   [ScenarioSelect.cs]
├─ Background                      ColorRect        Full Rect, mouse_filter Ignore
└─ Layout                          VBoxContainer    Full Rect
   ├─ TopBar                       PanelContainer
   │  └─ TopBarBox                 HBoxContainer    separation 12
   │     ├─ YearDisplay            PanelContainer   Theme-Variante YearPanel
   │     │  └─ YearBox             HBoxContainer
   │     │     ├─ YearCaption      Label            text „UI_START_YEAR“, Theme-Variante LabelMuted
   │     │     └─ YearLabel        Label            Theme-Variante HeaderLarge (Text setzt das Script)
   │     ├─ TopSpacer              Control          size_flags_h Expand+Fill
   │     ├─ ZoomToPointCheck       CheckButton      text „UI_ZOOM_TO_POINT“, button_pressed true
   │     ├─ MapModeCaption         Label            text „UI_MAP_MODE“, Theme-Variante LabelMuted
   │     └─ MapModeBar             HBoxContainer    separation 4 (Buttons werden zur Laufzeit eingefügt)
   ├─ Main                         HBoxContainer    size_flags_v Expand+Fill
   │  ├─ LeftColumn                VBoxContainer    custom_minimum_size x 380
   │  │  ├─ ScenarioListPanel      PanelContainer
   │  │  │  └─ ScenarioListBox     VBoxContainer
   │  │  │     ├─ ScenarioListCaption Label         text „UI_SCENARIOS“, Theme-Variante HeaderSmall
   │  │  │     └─ ListScroll       ScrollContainer  custom_minimum_size y 220, horizontal_scroll Disabled
   │  │  │        └─ ScenarioList  VBoxContainer    size_flags_h Expand+Fill, separation 4 (Einträge zur Laufzeit)
   │  │  └─ DetailPanel            PanelContainer   size_flags_v Expand+Fill
   │  │     └─ DetailMargin        MarginContainer  Ränder 16
   │  │        └─ DetailBox        VBoxContainer    separation 10
   │  │           ├─ PreviewImage  TextureRect      min size y 180, expand_mode Ignore Size, stretch_mode Keep Aspect Covered
   │  │           ├─ ScenarioTitle Label            Theme-Variante HeaderLarge, autowrap Word
   │  │           ├─ ScenarioTags  HFlowContainer   (Chips für Spezies, Region, Schwierigkeit, zur Laufzeit)
   │  │           ├─ ScenarioDescription RichTextLabel  bbcode_enabled true, size_flags_v Expand+Fill, scroll_active true
   │  │           └─ LockedHint    Label            visible false, Theme-Variante HintWarning, autowrap Word
   │  └─ MapView                   Control          size_flags_h/v Expand+Fill, clip_contents true, mouse_filter Stop   [MapPanZoom.cs]
   │     ├─ MapContent             Control          Position/Scale setzt das Script, mouse_filter Ignore
   │     │  └─ WorldMap            TextureRect      texture weltkarte, expand_mode Keep Size, texture_filter Linear Mipmap, mouse_filter Ignore
   │     ├─ MarkerLayer            Control          Full Rect, mouse_filter Ignore (Punkte zur Laufzeit)
   │     ├─ MapModeLabel           Label            Anchor Bottom Left, Abstand 12, Theme-Variante MarkerLabel (Name des Kartenmodus)
   │     └─ MapControls            VBoxContainer    Anchor Bottom Right, Abstand 16
   │        ├─ ZoomInButton        Button           text „+“ bzw. Icon, tooltip „UI_ZOOM_IN“
   │        ├─ ZoomOutButton       Button           text „−“, tooltip „UI_ZOOM_OUT“
   │        └─ ResetViewButton     Button           Icon Welt, tooltip „UI_RESET_VIEW“
   └─ BottomBar                    PanelContainer   custom_minimum_size y 110
      └─ BottomBox                 HBoxContainer    separation 12
         ├─ BackButton             Button           text „UI_BACK“, size_flags_v Shrink End, min size 160×48
         ├─ NewsScroll             ScrollContainer  size_flags_h Expand+Fill, vertical_scroll Disabled
         │  └─ NewsList            HBoxContainer    separation 8 (Karten zur Laufzeit)
         └─ ContinueButton         Button           text „UI_CONTINUE“, Theme-Variante ButtonPrimary, size_flags_v Shrink End, min size 200×48
```

Hinweise:
- **Reihenfolge in `MapView`** zählt: `MarkerLayer` liegt über `MapContent`, `MapControls` ganz oben. Die Punkte skalieren nicht mit dem Zoom. Das Script rechnet ihre Position aus Kartenpixel × Zoom + Verschiebung.
- `mouse_filter Ignore` auf Karte und Ebenen ist wichtig, sonst schluckt die Karte die Klicks, und `MapView` bekommt weder Ziehen noch Mausrad.
- Texte sind Übersetzungsschlüssel (Abschnitt 12). Godot übersetzt `Label.text`/`Button.text` automatisch, sobald T-035 steht; bis dahin siehst du die Schlüssel.
- Kartenkoordinaten in der Konfiguration sind Pixel der Originalkarte (4320×2160). Wird die Textur verkleinert importiert, rechnet das Script um.
- Alle Kartenmodus-Texturen müssen dieselbe Größe und Ausrichtung wie `weltkarte.png` haben (die `voelkerkarte.png` ist 4320×2560, siehe T-037).

## 5. Modul `ScenarioMarker.tscn` (Punkt auf der Karte)

```
ScenarioMarker                     Control          size 56×56, pivot_offset 28/28, mouse_filter Stop   [ScenarioMarker.cs]
├─ Ring                            Panel            Full Rect, Theme-Variante MarkerRing, mouse_filter Ignore
├─ Icon                            TextureRect      Anchor Center, size 32×32, expand_mode Ignore Size, stretch Keep Aspect Centered, mouse_filter Ignore
├─ LockIcon                        TextureRect      Anchor Bottom Right, size 20×20, visible false, mouse_filter Ignore
└─ NameLabel                       Label            unter dem Punkt (Position y 60), Theme-Variante MarkerLabel, align Center, mouse_filter Ignore
```

- Der Mittelpunkt des Markers liegt auf der Kartenposition.
- **Zustände** zeigt der `Ring` über vier StyleBoxes im Theme: `MarkerRingNormal`, `MarkerRingHover`, `MarkerRingSelected`, `MarkerRingLocked`. Die Icons sind schwarz mit Alpha und lassen sich per `Modulate` nicht einfärben (Schwarz × Farbe = Schwarz). Deshalb trägt der Ring die Farbe, das Icon bleibt schwarz auf hellem Grund.
- `tooltip_text` setzt das Script (Name + Schwierigkeit).

## 6. Modul `ScenarioListEntry.tscn` (Liste links)

```
ScenarioListEntry                  Button           toggle_mode true, button_group ScenarioButtonGroup.tres, size_flags_h Expand+Fill, min size y 44, Theme-Variante ScenarioListButton   [ScenarioListEntry.cs]
└─ EntryBox                        HBoxContainer    Full Rect, mouse_filter Ignore
   ├─ Icon                         TextureRect      size 28×28, expand_mode Ignore Size, stretch Keep Aspect Centered, mouse_filter Ignore
   ├─ NameLabel                    Label            size_flags_h Expand+Fill, text_overrun Ellipsis, mouse_filter Ignore
   └─ LockIcon                     TextureRect      size 16×16, visible false, mouse_filter Ignore
```

- `Button` + `ButtonGroup` liefert Normal/Hover/Gedrückt/Fokus über das Theme ohne Code, und genau ein Eintrag ist ausgewählt.
- Alle Kinder `mouse_filter Ignore`, damit der Button Klick und Doppelklick bekommt.
- Gesperrte Szenarien bleiben anklickbar (Details und Freischalt-Hinweis sollen sichtbar sein). Ausgegraut wird über `Modulate` auf `EntryBox`.

## 7. Modul `NewsCard.tscn` („neueste Szenarien“ unten)

```
NewsCard                           Button           toggle_mode false, custom_minimum_size 300×90, Theme-Variante NewsCardButton   [NewsCard.cs]
└─ NewsMargin                      MarginContainer  Full Rect, Ränder 8, mouse_filter Ignore
   └─ NewsBox                      VBoxContainer    mouse_filter Ignore
      ├─ NewsHeader                HBoxContainer    mouse_filter Ignore
      │  ├─ NewsBadge              Label            Theme-Variante TagChipLabel („UI_NEWS_NEW“ / „UI_NEWS_UPDATED“ / „UI_NEWS_PLANNED“), mouse_filter Ignore
      │  ├─ NewsTitle              Label            size_flags_h Expand+Fill, text_overrun Ellipsis, mouse_filter Ignore
      │  └─ NewsVersion            Label            Theme-Variante LabelMuted, mouse_filter Ignore
      └─ NewsText                  Label            autowrap Word, max_lines_visible 2, text_overrun Ellipsis, Theme-Variante LabelMuted, mouse_filter Ignore
```

- Klick wählt das zugehörige Szenario aus (wie in der Liste), Doppelklick = Weiter.
- Inhalt (Änderungen, Features) kommt aus der Szenario-Konfiguration; angezeigt werden die neuesten Einträge nach Datum.

## 8. Modul `MapModeButton.tscn` (Kartenmodus oben)

```
MapModeButton                      Button           toggle_mode true, button_group MapModeButtonGroup.tres, custom_minimum_size 40×40, expand_icon true, Theme-Variante MapModeButton   [MapModeButton.cs]
```

- Icon und `tooltip_text` (Name des Modus) setzt das Script aus der Kartenmodus-Konfiguration.

## 9. Theme `TiefbergTheme.tres` – Typ-Varianten

| Variante | Basistyp | Zweck |
|---|---|---|
| `HeaderLarge` | Label | Szenariotitel, Jahr |
| `HeaderSmall` | Label | Überschrift „Szenarien“ |
| `LabelMuted` | Label | Nebentexte |
| `HintWarning` | Label | Freischalt-Hinweis |
| `MarkerLabel` | Label | Text auf der Karte (mit Outline für Lesbarkeit) |
| `TagChipLabel` | Label | Badge „Neu“/„Geplant“ |
| `YearPanel` | PanelContainer | Rahmen der Jahresanzeige |
| `ButtonPrimary` | Button | Weiter |
| `ScenarioListButton` | Button | Eintrag in der Liste (normal/hover/pressed/focus) |
| `NewsCardButton` | Button | Karte unten |
| `MapModeButton` | Button | Kartenmodus (pressed = aktiv) |
| `MarkerRing` | Panel | Basis für den Ring |
| `TagChip` | PanelContainer | Chip in `ScenarioTags` |

Die vier Ring-Zustände (`MarkerRingNormal` …) liegen als StyleBoxFlat-Ressourcen im Theme (runde Ecken = halbe Größe → Kreis).

## 10. Was die Scripts erwarten (Exports)

Alle Referenzen laufen über `[Export]`, im Inspector zuweisen. Fehlt eine, loggt das Script einen Fehler und nutzt als Ersatz den Pfad aus dem Baum oben.

**`ScenarioSelect`** (an `ScenarioSelect`)
- `YearLabel`, `ZoomToPointCheck`, `MapModeBar`, `ScenarioList`, `NewsList`, `BackButton`, `ContinueButton`
- `PreviewImage`, `ScenarioTitle`, `ScenarioTags`, `ScenarioDescription`, `LockedHint`
- `MapView` (Typ `MapPanZoom`), `MarkerLayer`, `MapModeLabel`
- `MarkerScene`, `ListEntryScene`, `NewsCardScene`, `MapModeButtonScene`, `TagChipScene` (optional), je PackedScene
- `PlaceholderPreview` (Texture2D für Szenarien ohne eigenes Bild), `NewsCount` (int, Standard 3)

**`MapPanZoom`** (an `MapView`)
- `Content` (`MapContent`), `Map` (`WorldMap`), `ZoomInButton`, `ZoomOutButton`, `ResetViewButton`
- Einstellbar: `SourceMapSize` (4320×2160, Bezug der Konfigurationskoordinaten), `MaxZoom` 8, `ZoomStep` 1.15, `FocusZoom` 3, `FocusDuration` 0.4 s. Zoom ist relativ zur Gesamtansicht: 1 = ganze Karte sichtbar (Rest zentriert), 8 = achtfach.
- Methoden `FocusOn(sourcePixel)`, `SetMapTexture(texture)`, `SourceToView(sourcePixel)`; Signal `ViewChanged` → `ScenarioSelect` setzt daraufhin die Punkte neu
- `MapContent` bekommt vom Script Anker oben links und die Texturgröße; die Anker im Editor sind egal. Fehlende Exports ersetzt das Script über die Pfade aus Abschnitt 4 (mit Warnung im Log).

**`ScenarioMarker`**: `Ring`, `Icon`, `LockIcon`, `NameLabel`; Signale `Selected(id)`, `Activated(id)`
**`ScenarioListEntry`**: `Icon`, `NameLabel`, `LockIcon`, `EntryBox`; Signale `Selected(id)`, `Activated(id)`
**`NewsCard`**: `NewsBadge`, `NewsTitle`, `NewsVersion`, `NewsText`; Signale `Selected(id)`, `Activated(id)`
**`MapModeButton`**: keine Exports; Signal `ModeSelected(modeId)`

## 11. Verhalten (für den Test nach dem Bau)

| Aktion | Ergebnis |
|---|---|
| Mausrad über der Karte | Zoom zur Mausposition, begrenzt auf Min/Max |
| Ziehen mit linker oder mittlerer Maustaste | Verschieben, Karte verlässt die Ansicht nie ganz |
| Klick auf Punkt, Listeneintrag oder News-Karte | Auswahl überall synchron, Details und Jahr aktualisiert, ggf. Zoom zum Punkt |
| Doppelklick auf Punkt, Listeneintrag oder News-Karte | wie Weiter |
| Kartenmodus-Button | Karte wechselt die Textur, Zoom und Ausschnitt bleiben, `MapModeLabel` zeigt den Namen |
| Weiter / Enter | vorerst `GameManager.StartNewGame(scenarioId)`; bei gesperrtem Szenario nichts, Hinweis sichtbar |
| Zurück / Esc | Hauptmenü |
| Pfeil hoch/runter | vorheriges/nächstes Szenario in der Liste |
| Fenstergröße ändern | Karte passt sich an, Punkte bleiben an ihrem Ort |

## 12. Offen

- Ob „Weiter“ später einen Zwischenschritt öffnet (z. B. Charakter oder Optionen) statt direkt zu starten.
- Jahresanzeige: vorerst nur Anzeige des Startjahrs aus der Konfiguration. Ob man wie in EU4 ein Jahr wählen kann, ist offen. Das richtige Datum im Spezieskalender hängt an T-023 und T-025.
- Welche Kartenmodi es gibt (Vorschlag: Terrain, Klima, Vegetation, Völker aus `lore/`), siehe Abschnitt 4 und T-037.
- Ob „Zoom zum Punkt“ als Einstellung gespeichert wird (bis Optionen existieren: nur für die Sitzung).
- Freischaltbedingungen für gesperrte Szenarien; vorerst nur ein Text aus der Konfiguration.
