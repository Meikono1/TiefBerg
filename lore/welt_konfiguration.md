# Welt-Konfiguration – Generierungsdaten

Koordinaten: äquirektangulär, x = 0 → 180° W, y = 0 → 90° N. Angaben als (Länge, Breite) in Grad.

---

## 1. World Climate Lab – Einstellungen

Ausgangspunkt: **Voreinstellung „Erde“**, dann folgende Änderungen.

### Körperparameter

| Parameter | Wert | Erde |
|---|---|---|
| Achsneigung | **26°** | 23,4° |
| Rotationsperiode | **20,00 h** | 23,93 h |
| Gebundene Rotation | aus | aus |
| Radius | 6371 km | 6371 km |
| Masse | 1,0 M⊕ | 1,0 M⊕ |
| Schwerkraft | 9,81 m/s² | 9,81 m/s² |
| Umlaufzeit | 365,26 Tage | 365,26 Tage |
| Einstrahlung | 0,98 S⊕ | 1,0 S⊕ |

### Atmosphärenparameter

| Parameter | Wert | Erde |
|---|---|---|
| Oberflächendruck | **1,2 bar** | 1,0 bar |
| Hauptgas | Stickstoff (N₂) | N₂ |
| O₂ | **24 %** | 21 % |
| CO₂ | 0,0400 % | 0,0400 % |
| CH₄ | 0,0002 % | 0,0002 % |
| N₂O | Erdwert | |
| NH₃, SO₂, SF₆, CFC-11, PFCs | 0 | 0 |

Hinweis: Falls CO₂ wegen des höheren Drucks leicht gesenkt wurde, hier nachtragen.

Abgeleitet: O₂-Partialdruck 0,29 bar (Erde 0,21), Siedepunkt Wasser ≈ 107 °C.

### Topografie

1. „Topografie laden“ → `heightmap_16bit.png`
2. Importierte Gipfelhöhe: **leer lassen (AUTO, 8,85 km)**
3. „Maske hochladen“ → `meeresmaske.png` (Eigene Maske: AN, nicht invertieren)
4. Simulation zurücksetzen und bis „Klima eingefroren“ laufen lassen (ca. 30 Sim-Jahre)

---

## 2. Weltgeometrie – Grundlage

### Ableitung aus der Ursprungskarte

| Schritt | Parameter |
|---|---|
| Verkleinern | 20000 × 7960 → 5000 × 1990 (nearest) |
| Wasser erkennen | `(B − R > 40) und (B > G)` |
| Maßstabsleiste entfernen | Bereich y > 93 %, x > 85 % = Wasser |
| Landmaske glätten | Öffnung, 2 Iterationen |
| Breitenlage | Nordspitze 65° N, Südspitze 55° S → 14,725 px/Grad |
| Weltkanvas | 5301 × 2650 (2:1) |
| Kontinentabstand verdoppeln | Schnitt bei x = 1940, rechter Teil +320 px nach Osten |
| Arbeitsraster | auf 4096 × 2048 skaliert (11,38 px/Grad) |

### Nordwestland verschoben

Landkomponenten an (10° O, 40° N), (40° O, 45° N), (28° O, 30° N), (5° W, 20° N) samt Biomwerten **um 8° nach Norden** verschoben.

### Küstenlinie

- Signierte Distanz zur alten Küste (px): `score = clip(sd / 12, −4, 4)`
- Küstenrauschen: `score += fbm(64,32,16,8; seed 11) · (1,3 + 2,0 · hl) · exp(−|sd| / 45)` mit `hl = clip((|Breite| − 35) / 25, 0, 1)` (Fjorde in hohen Breiten)
- Blob-Rauschen: `fbm(64,32,16,8; seed 12) · 1,4`
- Domain-Warp der Blobs: `fbm(256,128,64,32,16; seeds 101/102) · 2,2°`, Stärke `k = min(1; 0,25 + 0,12 · min(a, b))`
- Blob-Funktion: Ellipse `1 − (u/a)² − (v/b)² + namp · Rauschen`, eingebracht mit Faktor 6 (ADD = max, CUT = min)
- Inseln < 4 px entfernt

---

## 3. Weltgeometrie – Änderungen

Format der Blobs: **(Länge, Breite, Halbachse O-W °, Halbachse N-S °, Drehung °, Rauschamplitude)**. Die Rauschamplitude ist 0,35, wenn keine angegeben ist.

### Land hinzugefügt

| Region | Blobs |
|---|---|
| Südspitze Westkontinent | (−110, −36, 7, 5, 0), (−105, −43, 5, 6, 20), (−100, −50, 3,5, 5, 25) |
| Karibik-Inselbogen | (−66, 27, 4, 1, −15, 0,6), (−60, 23, 1,5, 0,9, 0, 0,6), (−56, 19, 1, 0,8, 0, 0,6), (−55, 14, 0,9, 0,7, 0, 0,6), (−57, 10, 0,8, 0,7, 0, 0,6) |
| Polar-Archipel | (−135, 66, 5, 2,5, 10, 0,7), (−120, 68, 4, 2,5, −10, 0,7), (−105, 70, 5, 3, 0, 0,7), (−92, 67, 4, 2,2, 15, 0,7), (−110, 75, 6, 2,5, 0, 0,7), (−88, 74, 4, 2, 0, 0,7) |
| Eisinsel (Grönland-Analog) | (−34, 73, 13, 9, 0, 0,4) |
| Vulkaninsel | (−20, 64, 2,5, 1,4, 0, 0,4) |
| Westinseln | (−11, 59, 3, 3,5, 15, 0,4), (−16, 58, 1,6, 1,8, 0, 0,5) |
| Mittelozean-Inseln | (−36, 38, 0,9, 0,6, 0, 0,5), (−33, 36, 0,6, 0,5, 0, 0,5), (−31, −9, 0,7, 0,6, 0, 0,5), (−38, −40, 1,2, 0,7, 0, 0,5), (−30, −54, 2, 0,8, 0, 0,6) |
| Arktische Inseln Ost | (40, 73, 6, 2,5, 0, 0,6), (62, 76, 7, 2, 0, 0,6), (68, 68, 1,2, 4,5, 30, 0,5) |
| Großinsel (Madagaskar-Analog) | (140, −22, 3,2, 8, 15, 0,4) |
| Inselbogen (Japan-Analog) | (153, 37, 1,8, 6, −30, 0,5), (157, 29, 1,5, 3,5, −10, 0,5), (156, 23, 1, 2, 0, 0,6) |
| Tropen-Archipel | Borneo (162, 3, 6, 5, 0, 0,4), Neuguinea (166, −6, 8, 2,6, 5, 0,4), (153, 11, 1,4, 3,5, 20, 0,6), (156, −2, 1,5, 4, −30, 0,6), (150, −9, 6, 1,2, 0, 0,6), (160, −10, 2,5, 0,9, 0, 0,6), (170, 14, 1, 0,8, 0, 0,6), (173, 9, 0,8, 0,7, 0, 0,6) |
| Südland | (163, −34, 11, 8,5, 10, 0,35) |
| Doppelinsel (Neuseeland-Analog) | (−168, −46, 2,5, 4, 30, 0,5) |
| Hotspot-Inseln | (−163, 20, 1, 0,7, 0, 0,6), (−160, 18,5, 0,7, 0,6, 0, 0,6), (−158, 17, 0,5, 0,5, 0, 0,6), (−167, −14, 0,9, 0,6, 0, 0,6), (−165, −17, 0,6, 0,5, 0, 0,6) |
| Subpolare Inseln | (150, −52, 2, 1, 0, 0,6), (100, −58, 1,5, 0,8, 0, 0,6) |
| Weitere Inselgruppen (je Rauschen 0,5) | (−17, 28, 1,2, 0,8), (−13,5, 29,5, 0,8, 0,6), (−30, 16, 0,8, 0,6), (−27,5, 14, 0,6, 0,5), (−88, −51, 2,2, 1), (−48, 32, 0,6, 0,5), (−40, 2, 0,8, 0,6), (135, −5, 0,8, 0,6), (150, −24, 0,8, 0,7), (158, 46, 1, 0,6, 30), (162, 49,5, 1, 0,6, 30), (167, 52, 1,6, 0,6), (174, 53, 1,4, 0,6), (−171, 54, 1,4, 0,6), (−166, 55, 1,6, 0,7), (−155, −2, 1,2, 0,8) |

### Wasser hinzugefügt (CUT)

| Region | Blobs |
|---|---|
| Nordbucht | (−104, 52, 8, 5,5, 0) |
| Große Seen | (−104, 47, 2,2, 1,2, 10, 0,5), (−97, 45, 2,5, 1, −20, 0,5), (−93, 49, 1,8, 1,4, 0, 0,5) |
| Golf | (−75, 24, 7, 4, −10), (−62, 21, 6, 2, −5) |
| Mittelmeer Nordwestland | (8, 29, 12, 2,6, −4), (−6, 30, 5, 2, 10) |
| Binnenmeer Ostkontinent | (84, 21, 17, 5,5, −5) |
| Meerenge zur Ostbucht | (101, 16, 6, 2,8, −20) |
| Salzsee | (42, −18, 4, 2,5, 20, 0,4) |
| Grabenseen | (67, −4, 0,8, 3, 5, 0,3), (69, −12, 0,7, 2,8, 8, 0,3), (71, −19, 1,6, 1,4, 0, 0,4) |
| Südbucht | (55, −32, 6, 3,5, 0), (57, −44, 2,5, 7,5, 10) |

Ergebnis: Landanteil ≈ 31 % (flächengewichtet), kein Land näher als 4° an der Datumsgrenze.

---

## 4. Höhenmodell

### Grundrelief

- Küstenabstand `dc` in Grad
- `Höhe = 40 + 560 · (1 − exp(−dc / 6)) + 320 · fbm(128,64,32,16,8; seed 21) · (1 − exp(−dc / 1,5))`
- Alte Massive aus der Ursprungskarte: `clip((Biom_geglättet − 0,6) / 0,4)^1,6 · 2600 · (0,5 + 0,5 · ridged(seed 99)) · taper(3°)`

### Gebirgsketten

Format: **Name: Polylinie (Länge, Breite) – Gipfelhöhe – Breite**

| Kette | Polylinie | Höhe | Breite |
|---|---|---|---|
| Westkordillere | (−143,55) (−145,46) (−142,36) (−137,24) (−134,12) (−133,2) (−130,−10) (−123,−22) (−113,−32) (−106,−41) (−101,−50) | 6400 m | 2,4° |
| Ostbergland | (−86,50) (−81,41) (−78,33) (−80,18) (−84,8) | 2100 m | 2,6° |
| Nordschild | (−130,54) (−118,57) (−96,58) (−84,55) | 1100 m | 3,0° |
| Zentralkette | (32,29) (44,31) (56,33) (66,31) (78,29,5) (88,30) (95,35) (102,36) (110,31) (120,31) (130,31) (140,27) (146,25) | 8849 m | 2,6° |
| Hochplateau | (44,10) (52,14) (58,10) | 2600 m | 4,0° |
| Westhügel Nord | (12,70) (10,62) (8,53) (4,44) (1,37) | 1300 m | 2,5° |
| Südkette NWL | (−6,24) (4,25) (14,23) | 2600 m | 1,6° |
| Südanden | (−6,−8) (−7,−20) (−4,−32) (2,−42) | 4300 m | 2,0° |
| Grabenschultern | (64,4) (66,−8) (68,−18) (72,−26) | 2600 m | 3,2° |
| Ostküstenkette | (121,−2) (125,−12) (122,−23) | 2300 m | 2,2° |
| Südkamm | (35,−42) (60,−48) (90,−46) (103,−40) | 3000 m | 2,0° |
| Nordostkamm | (84,47) (92,45) (99,43) | 3200 m | 2,0° |
| Vulkaninsel | (−21,64) (−19,65) | 1900 m | 1,0° |
| Inselbogen | (152,41) (155,33) (157,27) | 3700 m | 1,2° |
| Borneo | (158,6) (163,2) (166,0) | 4100 m | 1,6° |
| Neuguinea | (160,−5) (166,−7) (172,−6) | 4800 m | 1,3° |
| Großinsel | (141,−15) (140,−23) (138,−29) | 2800 m | 1,4° |
| Südland-Osten | (172,−26) (174,−34) (171,−41) | 2200 m | 2,0° |
| Doppelinsel | (−170,−42) (−166,−49) | 3700 m | 1,1° |
| Arktis-West | (−112,72) (−104,76) | 2000 m | 1,8° |

### Regeln für Gebirge

- **Automatisch ins Inland:** Jeder Punkt wandert entlang des Küstenabstand-Gradienten, bis `Abstand ≥ 1,3 · Breite + 1,2°`.
- **Schmale Landstücke:** `Breite_eff = min(Breite, max(0,6; Reichweite / 1,6))`.
- **Profil:** `exp(−(d / Breite_eff)²) · (0,35 + 0,65 · ridged^1,3)`, ridged mit seed `200 + Index`.
- **Abflachung zur Küste:** Smoothstep über `max(1,2°; 0,9 · Reichweite)`.
- **Kombination:** Maximum aus allen Ketten und den alten Massiven, dann zum Grundrelief addiert.

### Sonderformen

- **Eisschild:** Bereich `blob(−34, 73, 13, 9) > −0,2`, Kuppel `3300 · (1 − exp(−(dc / 3,5)^1,6)) + 450 · ridged(seed 300) · exp(−dc / 1,2)`
- **Grabenbruch:** −900 m entlang (66,1) (68,−10) (71,−20), Breite 1°

### Export

- Maximum auf 8849 m normiert, Grauwert = Höhe / 8849, Meer = 0, Land mindestens 0,004
- 16 Bit, 8192 × 4096 (bikubisch hochskaliert), Maske aus Score > 0, Inseln < 12 px entfernt
- **Statistik:** Median ≈ 370 m, Mittel ≈ 640 m, 90 % unter ≈ 1500 m, 99 % unter ≈ 4700 m

---

## 5. Vereinfachte Klimazonen

**Quelle:** `02-climate_zones-raw.png`, auf 2160 × 1080 verkleinert (nächster Nachbar). Das Bild enthält nur Legendenfarben (aus `02-climate_zones-framed.png`), jede Farbe ist genau eine Köppen-Klasse; eine unbekannte Farbe bricht ab. Land wird über die Meeresmaske (`Version5 Meermaske.png`) bestimmt; Pixel, deren Farbe nicht zur Maske passt, bekommen die nächste passende Zone. Höhe aus `Version 5 HM.png`: Grauwert × 8849 m (Abschnitt 4, Export), Mittelwert je Zielpixel.

**Umsetzung:** `tools/klimazonen.py` erzeugt `data/world/klimazonen.bin` (Kennung `TBKZ`, uint32 Version, Breite, Höhe, dann ein Byte Zonen-ID je Pixel, Norden zuerst, ab 180° W) und `data/world/klimazonen.yaml` (Zonentabelle mit Namensschlüssel). Flächen werden in 4er-Nachbarschaft und über die Datumsgrenze hinweg verbunden.

Welche Ebene aus welchem Lauf stammt, steht in Abschnitt 6.

### Zuordnung und IDs

| ID | Zone | Köppen | Farbe (RGB) |
|---|---|---|---|
| 1 | Tropischer Regenwald | Af, Am | 15, 100, 35 |
| 2 | Savanne | Aw, As | 160, 195, 60 |
| 3 | Heiße Wüste | BWh | 240, 175, 90 |
| 4 | Kalte Wüste | BWk | 200, 170, 150 |
| 5 | Steppe | BSh, BSk | 235, 230, 150 |
| 6 | Mittelmeerklima | Csa, Csb, Csc | 215, 115, 70 |
| 7 | Subtropisch feucht | Cfa, Cwa | 40, 155, 110 |
| 8 | Gemäßigter Wald | Cfb, Cfc, Cwb, Cwc | 110, 195, 85 |
| 9 | Kontinental | Dfa, Dfb, Dwa, Dwb, Dsa, Dsb | 150, 125, 195 |
| 10 | Taiga | Dfc, Dfd, Dwc, Dwd, Dsc, Dsd | 30, 90, 80 |
| 11 | Tundra | ET | 165, 175, 155 |
| 12 | Eis | EF | 245, 248, 252 |
| 13 | Hochgebirge | Höhe ≥ 2500 m | 120, 95, 85 |
| 20 | Tropisches Meer | WW, WH, WE, WB | 35, 85, 165 |
| 21 | Riff / Flachmeer | siehe Regeln | 60, 175, 205 |
| 22 | Gemäßigtes Meer | WT | 35, 65, 115 |
| 23 | Kaltes Meer | WC | 70, 90, 110 |
| 24 | Packeis | WI, WS | 200, 215, 230 |
| 25 | Binnengewässer | Seen ohne Ozeanverbindung | 85, 140, 210 |

### Regeln

1. 5 × 5-Modusfilter auf Land
2. **Südkette grüner** (Landkomponente bei 4° O / 22° N, Breite < 29°), nur B-Klimate (Zonen 3 und 4):
   - Breite > 24,5° → Mittelmeerklima (6)
   - 17,5° bis 24,5° → Steppe (5)
   - < 17,5° → Savanne (2)
3. **Hochgebirge:** Höhe ≥ 2500 m → 13, außer Eisschild (Länge −52° bis −15°, Breite 58° bis 86°)
4. **Kleinflächen < 40 px** auflösen (nicht 13; Zone 12 nur außerhalb des Eisschilds)
5. **Meer:** Ordinalwerte (tropisch → gemäßigt → kalt → Packeis) geglättet (Gauß 3 × 5 px; umgesetzt als Sigma 3 px in der Breite, 5 px in der Länge, nur über Meer gewichtet)
6. **Riff:** tropisches Meer oder gemäßigtes Meer mit |Breite| < 28°, höchstens 10 px (≈ 1,7°) vor der Küste
7. **Binnengewässer:** Meerflächen ohne Verbindung zur größten Meeresfläche → 25

Ergebnis (2026-10-23): Zone 12 (Eis) kommt nicht vor. Das EF im Bild liegt nur auf Gipfeln über 2500 m und wird Hochgebirge, der Eisschild ist Tundra.

---

## 6. Läufe und Quellen

Das World Climate Lab erzeugt eine Welt nicht exakt gleich neu: Mit denselben Einstellungen weicht jeder Lauf ab. Die Rohdaten stammen deshalb aus zwei Läufen. Sie liegen zusammengeführt unter `lore/world/` (mit `.gdignore`, Godot importiert sie nicht); welcher Ordner aus welchem Lauf kommt, steht unten. Nicht benötigte Exporte sind gelöscht, die Originale liegen beim Entwickler unter Dokumente/World Climate Lab.

**Lauf 2026-10-10** ist der gültige Stand des Klimas. Von ihm gibt es nur die 13 Ebenen als Bilder: `lore/world/all-layers` (raw und framed); Zahlen und Aufnahmen über das Jahr gibt es von ihm nicht.

**Lauf 2026-10-23** ist ein späterer Lauf mit denselben Einstellungen. Aus ihm stammen in `lore/world/`: `climate-data` (Zahlen: Temperatur, Niederschlag, Luftfeuchte und Taupunkt je Monat, Schnee und Eis, Bodenfeuchte, Meeresströmung oben und in der Tiefe als u/v im Jahresmittel), `wind_circulation` und `surface_currents` (je 96 Aufnahmen über das Jahr) sowie die Einzelbilder `vegetation`, `ice_cover`, `soil_moisture`, `current_paths` und `energy_flow`. Wind als Zahlen lässt sich nicht exportieren. Nicht übernommen: seine 20 Ebenen (andere Zeichnung, nicht nach Farben auslesbar), Tiefenströmung, Temperatur- und Taupunkt-Aufnahmen (dieselben Werte stehen als Zahlen in `climate-data`).

Ähnlichkeit der Bilder zum Lauf 2026-10-10 (Korrelation): Temperatur 0,996, Niederschlag 0,993, Wind 0,82, Meeresströmung 0,69. Die Klimazonen stimmen an Land nur zu 61 % überein, weil die Köppen-Einteilung an ihren Schwellen empfindlich ist.

| Ebene | Quelle |
|---|---|
| Klimazonen | Lauf 2026-10-10 (Abschnitt 5) |
| Temperatur, Niederschlag, Luftfeuchte als Zahlen | Lauf 2026-10-23. Annahme: Die Abweichung zum gültigen Lauf ist vernachlässigbar |
| Wind | Lauf 2026-10-23, Aufnahmen über das Jahr (Stärke); Richtung aus Windgürteln nach Breitengrad (T-039) |
| Meeresströmung | Lauf 2026-10-23, Zahlen (Jahresmittel) und Aufnahmen über das Jahr |

Die Aufnahmen aus einem Lauf vom 2026-10-20 (Wind, Meeresströmung) sind durch Lauf 2026-10-23 ersetzt, weil dieser näher am gültigen Lauf liegt.

---

## 7. Hauptflüsse

Die Karte zeigt nur Hauptflüsse (Design-Referenz Abschnitt 4). Ein Fluss ist schmaler als eine Zelle; die Ebene speichert, durch welche Zellen er fließt und wie groß er ist, nicht wie breit er gezeichnet wäre.

Das Flussbild der Klimasimulation (`06-rivers-raw.png`, Lauf 2026-10-10) taugt nicht als Quelle: Es ist auf 1024 × 512 Zellen mit weichem Saum gezeichnet, ausgewertet ergibt es Ringe, Lücken und Sterne in Seen. Die Flüsse werden deshalb aus Höhe und Niederschlag berechnet; das Bild dient nur zum Vergleich.

**Quellen:** Höhe aus `Version 5 HM.png` (Abschnitt 4), von Hand vorgegebene Verläufe aus `lore/flussvorgaben.yaml`, Land, Meer und Binnengewässer aus `data/world/klimazonen.bin` (Abschnitt 5), Niederschlag und Jahresmitteltemperatur als Zahlen aus Lauf 2026-10-23 (Abschnitt 6). Raster wie die Klimazonen, 2160 × 1080.

**Verfahren** (`tools/fluesse.py`):

1. **Abfluss je Zelle:** Niederschlag minus Verdunstung nach Turc, `ET = P / √(0,9 + P²/L²)`, `L = 300 + 25 T + 0,05 T³` (P in mm/Jahr, T in °C; L mindestens 50). Mal Zellfläche (Planetenradius 6371 km) ergibt m³/s.
2. **Unruhe im Gelände:** Die Heightmap ist auf rund 60 % des Landes nahezu flach, dazu kommen gleichmäßige Hänge; dort liefen Flüsse als gerade, parallele Linien. Annahme: Rauschen mit Standardabweichung 100 m (Strukturen von 1–8 Zellen) und zufällige Reihenfolge auf gleicher Höhe, fester Startwert. 40 m ließ an steilen Küsten parallele Linien, 200 m überdeckt das Relief.
3. **Vorgegebene Verläufe:** Jeder Verlauf aus `lore/flussvorgaben.yaml` wird als Korridor (Radius 2 Zellen) 1000 m tief eingeschnitten, mit 2 m Gefälle je Zelle zur Mündung und dem Rauschen aus Schritt 2, damit er darin mäandert. Das Wasser folgt ihm; wie viel er führt, ergibt sich aus dem Einzugsgebiet. Der letzte Punkt muss im Meer oder in einem Binnengewässer liegen.
4. **Fließnetz:** Senken werden von der Küste aus aufgefüllt (Prioritätsflutung); jede Landzelle fließt in genau eine Nachbarzelle, die näher an der Mündung liegt. Kreise sind damit ausgeschlossen, jeder Fluss erreicht das Meer. Binnengewässer werden bis zum Überlauf gefüllt und fließen weiter ab.
5. **Sammeln:** Jede Zelle gibt ihren Abfluss flussabwärts weiter, einmal mit und einmal ohne Verdunstung.
6. **Hauptfluss:** Zelle mit mittlerem Abfluss ab 150 m³/s und einem Einzugsgebiet ab 20.000 km² (etwa 60 Zellen), nicht auf Meer oder Binnengewässer. Ohne Mindesteinzugsgebiet werden an sehr nassen Küsten schon kurze Küstenbäche zum Fluss, weil eine Zelle dort über 20 m³/s liefert (Annahme).
7. **Trockenfluss:** Ein Lauf, der ohne Verdunstung ein Hauptfluss wäre, mit Verdunstung aber unter 150 m³/s bleibt, ist ein Flussbett, das nur in der Regenzeit Wasser führt (Annahme). So bekommen Steppe und Wüste ihre Flussbetten.

**Größenklassen** (Annahme, eingestellt nach der Dichte im Vergleichsbild):

| ID | Klasse | Abfluss ab |
|---|---|---|
| 1 | Fluss | 150 m³/s |
| 2 | Großer Fluss | 1000 m³/s |
| 3 | Strom | 5000 m³/s |
| 4 | Trockenfluss | nur in der Regenzeit, siehe Schritt 7 |

**Format:** `data/world/fluesse.bin` (Kennung `TBFL`, uint32 Version, Breite, Höhe, dann ein Byte Größenklasse je Zelle, 0 = kein Fluss) und `data/world/fluesse.yaml` (Klassen mit Namensschlüssel und Mindestabfluss).

**Vorgaben** (`lore/flussvorgaben.yaml`): `wuestenstrom`, der Abfluss des Sees am Regenwald nach Südosten durch die Wüste in die Bucht im Süden des Ostkontinents.

Ergebnis (2026-10-23): 9.717 / 5.538 / 2.743 Zellen Fluss / großer Fluss / Strom, 11.261 Zellen Trockenfluss, größter Abfluss rund 57.000 m³/s.

- O - Maßstab: Die Zellfläche folgt dem Planetenradius der Klimasimulation (6371 km); Design-Referenz Abschnitt 4 nennt 5 km pro Pixel der Weltkarte, das entspräche einem kleineren Planeten. Abflüsse und Klassengrenzen hängen daran.
- O - Vorschlag: Ein einmaliger, riesiger Wasserfall als Naturwunder, von Hand gesetzt (Ort, Höhe, Breite), nicht berechnet: Bei 18 km je Zelle und glatter Heightmap entsteht keine Steilkante. Kandidat: großer Fluss (~1.200 m³/s), der bei Länge 71°, Breite 29° steil aus dem Gebirge der Draconik stürzt.
- O - Endseen: Alle Senken laufen über und münden ins Meer; Flüsse, die in einem abflusslosen See enden, gibt es so nicht.
