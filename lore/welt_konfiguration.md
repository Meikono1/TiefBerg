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
   - Die drei Grenzen (29°, 24,5°, 17,5°) laufen nicht als gerade Breitenlinie, sondern sind je um geglättetes Rauschen verschoben, höchstens ± 1,5° (O - Vorschlag, 2026-10-24; vorher harte Kante quer über die Halbinsel)
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

**Fließrichtung:** `data/world/fliessrichtung.bin` (Kennung `TBFR`, gleicher Kopf) hält für jede Landzelle, in welche Nachbarzelle sie im Fließnetz aus Schritt 4 abfließt: 0 = Meer, 1 = Nordwest, 2 = Nord, 3 = Nordost, 4 = West, 5 = Ost, 6 = Südwest, 7 = Süd, 8 = Südost, 9 = Küstenzelle, mündet direkt ins Meer. Binnengewässer haben ebenfalls eine Richtung, weil sie überlaufen. Ost-West läuft über die Datumsgrenze. Jede Landzelle erreicht so eine Mündung. Gebraucht für die Seifenlagerstätten (Abschnitt 10).

**Bilder** (bei jedem Lauf): `lore/fluesse.png` (Ergebnis), `lore/fluesse_vergleich.png` (Ergebnis neben `06-rivers-raw.png`), `lore/fliessrichtung.png` (Richtung als Farbton, Mündungen weiß).

**Vorgaben** (`lore/flussvorgaben.yaml`): `wuestenstrom`, der Abfluss des Sees am Regenwald nach Südosten durch die Wüste in die Bucht im Süden des Ostkontinents.

Ergebnis (2026-10-23): 9.717 / 5.538 / 2.743 Zellen Fluss / großer Fluss / Strom, 11.261 Zellen Trockenfluss, größter Abfluss rund 57.000 m³/s.

- O - Vorschlag: Ein einmaliger, riesiger Wasserfall als Naturwunder, von Hand gesetzt (Ort, Höhe, Breite), nicht berechnet: Bei 18 km je Zelle und glatter Heightmap entsteht keine Steilkante. Kandidat: großer Fluss (~1.200 m³/s), der bei Länge 71°, Breite 29° steil aus dem Gebirge der Draconik stürzt.
- O - Endseen: Alle Senken laufen über und münden ins Meer; Flüsse, die in einem abflusslosen See enden, gibt es so nicht.

---

## 8. Raster, Spielfeld und gemeinsame Regeln der Natur-Ebenen

**Maßstab** (Design-Referenz Abschnitt 4): Planetenradius 6371 km. Das Spielfeldraster ist die Heightmap mit 8192 × 4096, ein Pixel ≈ 4,9 km am Äquator. Alle Weltdaten liegen auf dem Raster der Klimazonen mit 2160 × 1080, eine Zelle ≈ 18,5 km am Äquator. Die echte Zellbreite skaliert mit `cos(Breite)`; Flächenanteile, Abdeckungen und Kalibrierung werden deshalb immer nach Fläche gewichtet, sonst wären polnahe Gebiete überrepräsentiert. Abstände in den Regeln stehen in Kilometern, nicht in Zellen, damit sie nicht an der Auflösung hängen.

**Spielfeld:** Die Werte eines gewählten Pixels werden aus den umliegenden Zellen der Weltdaten berechnet (O - Verfahren, Design-Referenz Abschnitt 4). Was in dem Feld konkret liegt, setzt die Spielwelt beim Betreten.

**Pipeline:** Jede Stufe liest nur die Eingaben und die Ergebnisse früherer Stufen und schreibt eigene Dateien, damit sie einzeln neu erzeugt werden kann.

| Stufe | Werkzeug | Ergebnis | Abschnitt |
|---|---|---|---|
| Klimazonen und Köppen-Klassen | `tools/klimazonen.py` | `klimazonen.bin`, `koeppen.bin` | 5, 8 |
| Flüsse und Fließrichtung | `tools/fluesse.py` | `fluesse.bin`, `fliessrichtung.bin` | 7, 10 |
| Gesteinsräume | `tools/gesteinsraeume.py` | `gesteinsraeume.bin` | 9 |
| Grundgestein und Gesteine | `tools/gestein.py` | `gestein.bin` | 10 |
| Bodenfeuchte und Böden | `tools/boeden.py` | `bodenfeuchte.bin`, `boeden.bin` | 11 |
| Lagerstätten | `tools/lagerstaetten.py` | `lagerstaetten.bin` | 10 |
| Pflanzen | `tools/pflanzen.py` | `pflanzen.bin` | 12 |

Die Lagerstätten laufen nach den Böden, weil einige von ihnen vom Boden oder vom Feuchtgebietsanteil abhängen (Laterit-Eisen, Bauxit, Ocker, Raseneisenerz, Torf); die Böden brauchen dagegen nur das Gestein. Gesteine sind die Einträge aus `materialien_und_boeden.md` Abschnitt 5.1, Lagerstätten die aus 5.2 bis 5.6.

**Inhalte und Ergebnisse:**

- Die Regeln je Material, Boden und Pflanze sind Inhalte und stehen als YAML unter `data/nature/` (`materialien.yaml`, `boeden.yaml`, `pflanzen.yaml`). Sie sind Eingabe der Werkzeuge und zugleich die Metadaten fürs Spiel (Name, Kategorie, Fruchtbarkeit, Nutzung). Neue Einträge brauchen keine Codeänderung. Arbeitsstand der Listen: `lore/materialien_und_boeden.md`, `lore/pflanzen.md`.
- Ergebnisse liegen unter `data/world/`. Ebenen-Dateien mit vielen Layern (`gestein.bin`, `lagerstaetten.bin`, `boeden.bin`, `pflanzen.bin`, `bodenfeuchte.bin`): Kennung, uint32 Version, Breite, Höhe, Anzahl Layer, dann je Layer die ID (uint16 Länge + UTF-8) und die Länge der gepackten Daten, danach die Layer, je ein Byte pro Zelle (Norden zuerst, ab 180° W), einzeln mit zlib gepackt. Seltene Materialien sind fast überall 0 und werden dadurch sehr klein. Ebenen mit einem Wert je Zelle (`koeppen.bin`, `gesteinsraeume.bin`, `fliessrichtung.bin`) folgen dem Format von `klimazonen.bin`.
- Ein Layer reicht von 0 bis 255: 0 = kein Vorkommen, 255 = ergiebigste Stelle dieses Eintrags auf dem Planeten (Normalisierung je Eintrag). Meer ist immer 0. Wie selten ein Eintrag insgesamt ist, sagt seine Seltenheitsklasse.
- **Bilder:** Jeder Lauf schreibt Übersichtskarten nach `lore/` (Land und Meer als Hintergrund) und je Layer ein Graustufenbild nach `lore/ebenen/<kategorie>/<id>.png`. `lore/ebenen/` hat ein `.gdignore`, damit Godot die vielen Bilder nicht importiert.

**Regeln für alle Werkzeuge:**

- Fester Startwert der Welt im Werkzeug. Startwert je Eintrag = Hash aus Weltstartwert und ID (bei Linien zusätzlich die Landmasse). Gleiche Eingaben ergeben gleiche Karten.
- Logging je Stufe: Start, Ende, Laufzeit. Je Layer: ID, Zielabdeckung, erreichte Abdeckung, Maximum und Mittelwert vor der Normalisierung, Schwellwert, Anzahl Flecken. Warnung bei leerer Karte, fehlender Abhängigkeit und Abweichung über Faktor 2 vom Ziel.
- Abhängigkeiten zwischen Einträgen werden als Graph aufgelöst (topologische Sortierung), Zyklen brechen mit Fehler ab.
- Unbekannte IDs in den YAML-Daten (Köppen-Klasse, Boden, Gesteinsraum) brechen ab, statt geraten zu werden.

### Köppen-Raster

`tools/klimazonen.py` schreibt zusätzlich `data/world/koeppen.bin` (Kennung `TBKP`, Kopf wie `klimazonen.bin`): ein Byte Köppen-Klasse je Zelle, Nummern wie in `lore/world/climate-data/climate_classes.json` (1 = Af bis 31 = EF, Meer 32–39), dazu `data/world/koeppen.yaml` (Nummer, Code, Zone, Farbe) und das Bild `lore/koeppen.png` in den Legendenfarben. Die Klimazonen bleiben dabei unverändert.

Verfahren, aus derselben Quelle wie die Zonen:

1. Verkleinern und Abgleich mit der Meeresmaske wie die Zonen; die Klasse kommt vom selben Pixel wie die Zone.
2. 5 × 5-Modusfilter auf Land über die Köppen-Klassen.
3. „Südkette grüner“ setzt die Klassen passend zur neuen Zone: Mittelmeer → Csa, Steppe → BSh, Savanne → Aw (O - Vorschlag).
4. **Angleichen:** Wo Modusfilter, Kleinflächen oder Meeresglättung die Zone geändert haben, passt die Klasse nicht mehr zur Zone; sie wird durch die Klasse des nächsten Pixels ersetzt, die zur Zone gehört. Danach gehört jede Klasse zu ihrer Zone.
5. Hochgebirge, Riff und Binnengewässer haben keine eigenen Klassen; dort bleibt die Klasse aus dem Bild (im Hochgebirge meist ET, EF oder die Klasse des Umlands).

Kleinflächen werden nur für die Zonen aufgelöst, nicht für die Klassen: Innerhalb einer Zone dürfen kleine Flecken einer anderen Klasse derselben Zone bleiben.

Ergebnis (2026-10-24): 27 Landklassen kommen vor (Dsd, Dwd und Dfd nicht), am häufigsten BSh, Cfa, BWh und Dfb; EF liegt nur auf 544 Gipfelzellen.

Seit T-075 schreibt `tools/klimazonen.py` außerdem bei jedem Lauf `lore/klimazonen.png`.

**Eisschild:** Der Eisschild wird über die Maske aus Abschnitt 5 bestimmt (Länge −52° bis −15°, Breite 58° bis 86°, Land), nicht über EF; EF liegt nur auf Gipfeln.

---

## 9. Gesteinsräume

Ein Gesteinsraum beschreibt die geologische Herkunft einer Landzelle und bestimmt, welche Gesteine und Lagerstätten dort zusammen auftreten. Er ist ein Zwischenschritt und kein Gebiet im Spiel (Design-Referenz Abschnitt 4). Die Typen und ihre Gesteine stehen in `lore/materialien_und_boeden.md`.

Es gibt keine Plattenkarte und keine automatische Erkennung aus der Topografie. Die Gebirge und Inselgruppen der Welt sind in den Abschnitten 3 und 4 festgelegt; jeder Kette und Gruppe wird von Hand ein Typ zugewiesen (Zuordnung: `lore/materialien_und_boeden.md` Abschnitt 2.1, bestätigt 2026-10-24).

**Eingabe:** `lore/gesteinsraeume.yaml`: je Kette Name, Polylinie und Breite (wie Abschnitt 4) und der Typ; je Inselgruppe die Blob-Mittelpunkte (wie Abschnitt 3) und der Typ; dazu der Grabenbruch als `GRABEN` und die Eisschild-Maske als `EISSCHILD`.

**Verfahren** (`tools/gesteinsraeume.py`):

**Festland** sind die Landmassen ab 2,5 Mio. km² (Ostkontinent, Westkontinent, Nordwestland, Südland), alles andere ist Insel.

1. **Ketten:** Die Polylinien in Abschnitt 4 sind die Linien vor dem automatischen Verschieben ins Inland und liegen teils im Meer. Eine Kette umfasst deshalb das erhöhte Gelände nahe ihrer Linie, nicht einen festen Streifen: Abstand zur Linie bis `2 · Breite`, und die Zelle liegt mindestens 200 m über dem Umland (Höhe geglättet über 3°) oder absolut über 1500 m. Der Grabenbruch ist eine Senke und nimmt seine Zellen ohne Höhenbedingung. Bei Überlappung gilt die nähere Kette.
2. **Inselgruppen:** Alle Inseln mit Land im Umkreis von 2° um einen Blob-Mittelpunkt einer Gruppe bekommen deren Typ (ein Blob kann in mehrere Inseln zerfallen sein). Inseln, auf denen eine Kette vom Typ `INSELBOGEN` oder `HOTSPOT` liegt, werden ganz zu diesem Typ. Kettenzellen behalten ihren Typ.
3. **Erhebungen ohne Kette:** Zusammenhängendes Land ab 1000 m (mindestens 30 Zellen), das zu keiner Kette gehört. Grenzt es an eine Kette, ist es deren Flanke und bekommt den Typ der nächsten Kettenzelle, sonst wird es `ALTGEBIRGE`.
4. **Übrige Inseln:** Eine Insel ohne Gruppe und ohne Kette wird `HOTSPOT`, wenn sie über 500 km vom nächsten Festland liegt und nicht größer als 100.000 km² ist. Größere Inseln weit draußen sind abgetrennte Kontinentstücke und bleiben beim Standard.
5. **OPHIOLITH:** In einem Saum von 2 Zellen am Rand von `KOLLISION` werden 35 % zu Ophiolith, ausgewählt über geglättetes Rauschen (fester Startwert).
6. **BECKEN:** Tiefland unter 400 m, das entweder bis 400 km vor `KOLLISION` oder `KUESTENBOGEN` auf deren Landseite liegt (näher am Gebirge als am Meer), bis 150 km an einem Binnengewässer, oder in einer Senke mindestens 50 m unter ihrem Überlauf (Prioritätsflutung ohne Geländerauschen).
7. **EISSCHILD:** Eisschild-Maske.
8. **KRATON:** alle übrigen Landzellen.
9. Gespeichert wird der Typ je Zelle. Weiche Übergänge entstehen erst beim Gestein, indem die Typmasken über einige Zellen geglättet werden.

**Ausgabe:** `data/world/gesteinsraeume.bin` (Kennung `TBGR`, Kopf wie `klimazonen.bin`, ein Byte Typ je Zelle, 0 = Meer oder Binnengewässer), `data/world/gesteinsraeume.yaml` (Typen mit ID, Schlüssel und Farbe; ohne Namensschlüssel, weil Gesteinsräume im Spiel nicht angezeigt werden), Bild `lore/gesteinsraeume.png`. IDs: 1 Kraton, 2 Becken, 3 Altgebirge, 4 Kollision, 5 Ophiolith, 6 Küstenbogen, 7 Inselbogen, 8 Graben, 9 Hotspot, 10 Eisschild.

Ergebnis (2026-10-24), Anteil am Land flächengewichtet: Kraton 74,2 %, Becken 6,3 %, Kollision 4,8 %, Altgebirge 3,8 %, Küstenbogen 3,1 %, Inselbogen 2,4 %, Graben 2,2 %, Hotspot 1,9 %, Eisschild 1,0 %, Ophiolith 0,3 %.

- Alle Zahlen oben sind Annahmen, eingestellt am ersten Bild.
- O - Zwei Punkte des Karibik-Inselbogens, (−66, 27) und (−60, 23), liegen auf dem Festland des Westkontinents (die Blobs sind angewachsen) und werden dort nicht zum Inselbogen.
- O - Zwischen Ketten und Becken bleibt ein schmaler Saum Kraton (Vorgebirge zwischen 400 und 1000 m). Er verschwimmt beim Gestein durch die weichen Übergänge; sonst könnte man ihn der Kette zuschlagen.
- O - Ostküstenkette und flache Küstenebenen: siehe `lore/materialien_und_boeden.md` Abschnitt 7.

---

## 10. Gestein und Lagerstätten

**Grundgestein** (`tools/gestein.py`, Daten `data/nature/materialien.yaml`): Jeder Gesteinsraum liefert Gesteinsanteile in Prozent (`rock_shares`). Je Gestein entsteht ein Anteilslayer, die Summe aller Gesteinsanteile je Landzelle ist 255. Verfahren (Annahmen, eingestellt am ersten Bild):

1. **Weiche Übergänge:** Gewicht je Gesteinsraum = Gauß-geglättete Raummaske (Sigma 1,5 Zellen), normiert auf Summe 1; die Anteile mischen sich nach diesen Gewichten.
2. **Schwankung:** Jeder Anteil × `exp(0,5 · Rauschen)` (Strukturen um 8 Zellen, fester Startwert je Gestein), danach wieder auf Summe 1.
3. **Bedingungen** (`materialien_und_boeden.md` Abschnitt 3): Gips bleibt nur in BW/BS oder bis 50 km an Binnengewässern, sonst wird er Kalkstein. Kreide wird Kalkstein, wenn das nächste Becken ganz aus Wüste (BWh, BWk) besteht. Im Altgebirge wird dort, wo Granit mindestens 30 % und Kalkstein mindestens 10 % ausmacht, die Hälfte des Kalksteins zu Marmor (Kontaktsaum).
4. **Bytes:** Anteile × 255, abgerundet; die fehlenden Bytes bekommen die größten Reste. Summe je Landzelle genau 255.

Anteile werden nicht kalibriert: Ihre Seltenheitsklasse ist nur Vergleichswert. Als Abdeckung zählt die Fläche, auf der ein Gestein mindestens 10 % ausmacht.

**Vorkommen** (`rule.kind: occurrence`): Eignung = Gewicht der genannten Gesteinsräume (`rooms`) × Wert der Quelle (`source`, ab `min_source`; mit `source_rooms` gilt die Quelle nur in diesen Räumen) × Rauschen der Verteilung. Rauschen: `FLAECHIG` geglättet über 10 Zellen, `KLUMPIG` und `ABGELEITET` über 3 Zellen (je `exp` des Rauschens), `PUNKTUELL` zufällige Zentren (0,2 % der Zellen) mit Radius 1,5 Zellen. Im Eisschild sind Vorkommen 0. Layer: über dem Schwellwert 1–255 (255 = höchster Wert), darunter 0.

**Verteilungsarten:**

| Verteilung | Erzeugung |
|---|---|
| `FLAECHIG` | Gesteinsanteil bzw. Bedingung × niederfrequentes Rauschen |
| `KLUMPIG` | Raummaske × Bedingung × Rauschen mit hohem Schwellwert |
| `PUNKTUELL` | N zufällige Zentren in geeigneten Zellen, radialer Abfall |
| `SEIFE` | flussabwärts einer Quelle angereichert, siehe unten |
| `ABGELEITET` | Quell-Layer × Bedingung × Rauschen |

Allgemein: `wert = raumGewicht(gesteinsraum) × bedingung(köppen, gestein, wasser, höhe) × rauschen(startwert, frequenz, schwelle)`, danach Kalibrierung und Normalisierung.

**Kalibrierung:** Die Seltenheitsklasse gibt eine Zielabdeckung vor (Zielwerte, Design-Referenz Abschnitt 4; Zahlen je Klasse in `materialien.yaml`, `rarity_classes`: 30 / 10 / 2,2 / 0,3 / 0,03 / 0,005 %, die geometrische Mitte jeder Klasse). Der Schwellwert je Material wird so gesetzt, dass die flächengewichtete Abdeckung der Landzellen mit Wert > 0 dem Ziel entspricht. Weicht sie danach um mehr als Faktor 2 ab (z. B. weil der passende Gesteinsraum fehlt), wird gewarnt und der Layer trotzdem geschrieben.

**Gemeinsame Hilfen:** `tools/ebenen.py` (Raster lesen, Ebenen-Dateien schreiben und lesen, Flächen, Rauschen, Einzelbilder), genutzt von allen Natur-Werkzeugen ab den Gesteinsräumen.

Ergebnis Gestein (2026-10-24): `gestein.bin` mit 21 Layern (15 Anteile inkl. Gips, 6 Vorkommen), 2,0 MB. Der Kraton hat 10 % Kalkstein als dünne Decke auf dem alten Kern (Entscheidung 2026-10-24); vorher war Kalkstein mit 4 % im Mittel viel seltener als seine Klasse. Mittlere Anteile am Land: Granit 25 %, Gneis 21 %, Sandstein 14 %, Kalkstein 11 %, Quarzit 8 %, Basalt 7 %, Tonschiefer 6 %, Rest unter 3 %. Alle Vorkommen treffen ihr Ziel (Obsidian 92 Fundorte, Bimsstein 91, Travertin 79, Speckstein 63, Alabaster 37, weißer Marmor 19). `lore/hauptgestein.png` hat rechts eine Legende mit dem Flächenanteil, auf dem jedes Gestein vorherrscht (Hilfe `with_legend` in `tools/ebenen.py`).

**Seifenlagerstätten:** Schwere Minerale (Gold, Zinn, Platin, Edelsteine) werden aus ihrer Quelle ausgewaschen und flussabwärts angereichert. Die Fließrichtung kommt aus dem Fließnetz von `tools/fluesse.py` (Abschnitt 7), das dafür zusätzlich `data/world/fliessrichtung.bin` schreibt (Richtung zur Nachbarzelle je Landzelle).

1. Quellwert = Wert des Primär-Layers (z. B. `gold_primaer`).
2. Von jeder Quellzelle flussabwärts laufen und `wert += quellwert × abfall^(strecke / 100 km)` addieren (O - Vorschlag: Abfall 0,5 je 100 km).
3. Abbruch an Meer, Binnengewässer oder unter einem Mindestwert.
4. Trockenflüsse mit halbem Faktor, weil sie weniger transportieren.
5. Bonus in Flachstrecken nach einem steilen Abschnitt.

**Zugänglichkeit:** Im `EISSCHILD` sind alle Lagerstätten 0. Der Layer zeigt das oberflächennah zugängliche Vorkommen.

**Ausgabe:** `data/world/gestein.bin` (Kennung `TBGS`, `tools/gestein.py`) mit Bild `lore/hauptgestein.png` (vorherrschendes Gestein je Zelle) und `data/world/lagerstaetten.bin` (Kennung `TBLA`, `tools/lagerstaetten.py`, nach den Böden) mit Bild `lore/lagerstaetten.png` (ausgewählte Lagerstätten farbig über dem Land); Einzelbilder unter `lore/ebenen/gestein/` und `lore/ebenen/lagerstaetten/`.

**Meteoreisen** hängt an keinem Gesteinsraum: wenige Einschläge, gleichmäßig über das Land gestreut (flächengewichtet).

- O - Weitere Faktoren der Zugänglichkeit (Tiefe, Bedeckung durch Sedimente) und ob tiefe Lagerstätten später gesondert betrachtet werden.
- O - Zielabdeckungen nach den ersten Läufen kalibrieren.

---

## 11. Bodenfeuchte und Böden

Werkzeug `tools/boeden.py` (Bodenfeuchte in `tools/bodenfeuchte.py`), Inhalte in `data/nature/boeden.yaml`, Lauf rund 17 s. Alle Zahlenwerte unten sind O - Vorschlag (im Werkzeug als Annahmen markiert) und werden an den Bildern abgestimmt.

### Bodenfeuchte

**Quellen** (Lauf 2026-10-23, Zahlen, 2048 × 1024, auf das Raster umgerechnet): `soil_water_index` (Bodenwasser, 0–1), `rainfall_annual` (Regen). Dazu die Hangneigung aus der Heightmap, Flüsse und Binnengewässer.

**Ergebnis je Zelle:**

- **Bodenfeuchte** 0–255 = 0,7 × Bodenwasser + 0,3 × Regenindex, Regenindex = 1 − exp(−Regen / 1000 mm). Der Index allein unterschätzt die wechselfeuchten Tropen (Aw im Mittel 0,23 bei 1160 mm Regen).
- **Angleich an die Köppen-Klasse:** Wo die Regeln aus Abschnitt 5 die Klasse geändert haben (vor allem „Südkette grüner“), passen Bodenwasser und Regen der Klimasimulation nicht mehr. Dort wird die Bodenfeuchte um Median(neue Klasse) − Median(alte Klasse) verschoben, die Mediane über alle unveränderten Zellen, die Verschiebung mit 3 Zellen geglättet. Alte Klasse = Farbe in `02-climate_zones-raw.png`. Betrifft 16.550 Zellen, im Mittel +0,065.
- **Feuchtgebietsanteil** 0–255 = Feuchte × Flachheit × Wassernähe. Feuchte steigt von Bodenfeuchte 0,55 bis 0,85 auf 1, Flachheit fällt bis zu einem Gefälle von 0,4 % auf 0, Wassernähe klingt mit 40 km um ganzjährige Flüsse und 30 km um Binnengewässer ab.
- **Wasserkategorien** der Pflanzen (Bedeutung in `lore/pflanzen.md`) als Anteile:
  - `TROCKEN` unter Bodenfeuchte 0,15, `FEUCHT` über 0,55, `FRISCH` dazwischen, jeweils mit weichem Übergang ± 0,05. `NASS` = Feuchtgebietsanteil; die vier ergeben zusammen 255.
  - `UFER` an ganzjährigen Flüssen: Anteil der Flusszelle 25 / 40 / 55 % je Klasse 1–3, Streifen 10 / 25 / 50 km. An Binnengewässern 40 % in der Nachbarzelle, klingt über 30 km ab.
  - `OASE` an Trockenflüssen und an ganzjährigen Flüssen in BW/BS: 40 %, klingt über 20 km ab.
  - `KUESTE` 40 % in Zellen direkt am Meer, eine Zelle weiter 0.
  - `STEHEND` = die Binnengewässer selbst (255). Das ist der einzige Layer mit Werten außerhalb des Landes.
  - TODO: `GEZEITEN`: Die Gezeiten liegen nur als Bild vor (`07-tides-raw.png`, Lauf 2026-10-10), Quelle offen. Bis dahin gibt es den Layer nicht.

Eine Zelle kann mehrere Kategorien haben (z. B. `FRISCH` und `UFER`).

**Ausgabe:** `data/world/bodenfeuchte.bin` (Kennung `TBBF`, Layer `bodenfeuchte`, `feuchtgebiet`, `TROCKEN`, `FRISCH`, `FEUCHT`, `NASS`, `UFER`, `OASE`, `KUESTE`, `STEHEND`), Bild `lore/bodenfeuchte.png` (vorherrschende Feuchtekategorie), Einzelbilder unter `lore/ebenen/bodenfeuchte/`.

**Stand 2026-10-24:** vorherrschend Trocken 23,6 %, Frisch 49,4 %, Feucht 26,2 %, Nass 0,8 % des Landes; Feuchtgebietsanteil auf 16 % des Landes über 0.

### Böden

**Verfahren** (Regeln je Boden in `lore/materialien_und_boeden.md` Abschnitt 6, als Daten in `boeden.yaml`):

1. **Zonaler Grundwert** aus der Köppen-Klasse: Hauptboden 70 %, der Rest zu gleichen Teilen auf die Nebenböden. Ein Nebenboden mit Bedingung („in Senken“, „auf Kalk“, „auf Sand“, „feucht“) bekommt seinen Anteil nur dort, sonst geht er an den Hauptboden. Ebenso kann ein anderer Boden unter einer Bedingung an die Stelle des Hauptbodens treten (Cfa: Rotgelberde, wo es dauerhaft feucht ist; Dfa: Braunerde statt Schwarzerde, wo es dauerhaft feucht ist). Kalk bzw. Sand: Anteil von Kalkstein + Marmor bzw. Sandstein ab 25 %. Feucht: Bodenfeuchte `FEUCHT` vorherrschend. Senke: Zelle liegt mindestens 10 m unter dem Überlauf ihrer Senke (Höhe von der Küste her aufgefüllt; 6,7 % des Landes).
2. **Übergang Schwarzerde** (Waldsteppe): BSk-Zellen bis 250 km vor Dfa/Dfb/Dwa/Dwb/Cfa/Cfb und umgekehrt bekommen Schwarzerde als Hauptboden (Streifen 500 km). Cfa/Cfb gehören dazu, weil auch der Übergang von kühler Steppe zu feuchtem Grasland tiefe Humusböden bildet.
3. **Überschreibungen** in fester Reihenfolge. Jede Regel setzt ihren Boden auf einen Anteil der Zelle, die übrigen schrumpfen im selben Verhältnis:
   - Gestein: Ist das vorherrschende Gestein Kalkstein oder Marmor, wird der Kalkanteil zu Terra rossa (Cs) bzw. Rendzina. Basalt, Andesit oder Tuff → Vulkanboden nur in jungen Vulkangebieten: im Gesteinsraum `HOTSPOT` voll, in `INSELBOGEN` und `KUESTENBOGEN` mit 40 % der Stärke (Asche fällt um die einzelnen Vulkane, nicht über den ganzen Bogen). Alter Basalt in allen anderen Gesteinsräumen verwittert zu Vertisol. Sandstein bei Bodenfeuchte `FEUCHT` → halber Sandsteinanteil Podsol. Rendzina, Vulkanboden und Vertisol nicht in BW und E (brauchen Feuchte zur Bildung).
   - Windablagerungen: Sandmeere auf 25 % der BWh-Fläche (geglättetes Rauschen, Strukturgröße 6 Zellen), dort 70 % Sandboden. Löss siehe unten.
   - Gewässer: Flussauen breiter als der `UFER`-Streifen, damit große Flüsse fruchtbare Täler bekommen: Anteil der Flusszelle 40 / 60 / 80 % je Klasse 1–3, Streifen 30 / 60 / 100 km; Feuchtgebietsanteil → Gley, in kühlen Klassen (Cfc, Dfb, Dfc, Dfd, Dsc, Dsd, Dwb, Dwc, Dwd, ET) Moor; Senken in BW/BS → 50 % Salzboden; Trockenflüsse → 30 % Sandboden.
   - Küste: Zellen am Meer mit Gefälle unter 0,2 %: in BW/BS 30 % Salzboden, sonst über Sandstein oder im Becken 30 % Sandboden.
   - Hangneigung: mittleres Gefälle der Heightmap-Pixel einer Zelle (8192 × 4096). Fels-Anteil steigt von 0 bei 1 % bis 80 % bei 3 % Gefälle. Die Pixel sind 4,9 km breit, das Gefälle bleibt daher klein: 90 % des Landes liegen unter 0,9 %, 99 % unter 2,6 %.
   - Hochgebirge (Zone 13, ab 2500 m): zusätzlich 50 % Fels, weil die Hangregel steile Hänge auf dieser Auflösung nicht sieht.
   - Eis: Gesteinsraum `EISSCHILD` und EF → ganz Eis.

**Ausgabe:** `data/world/boeden.bin` (Kennung `TBBO`, je Boden ein Anteilslayer, Summe je Landzelle 255, dazu ein Layer `hauptboden` mit der Position des Bodens in `boeden.yaml` ab 1, 0 = Wasser), Bild `lore/hauptboden.png`, Einzelbilder unter `lore/ebenen/boeden/`.

**Stand 2026-10-24** (vorherrschend, Anteil am Land): Braunerde 22,6 %, Kastanienerde 16,1 %, Rotgelberde 13,1 %, Wüstenrohboden 9,5 %, Roterde 8,1 %, Laterit 6,6 %, Auenboden 4,8 %, Fels 4,7 %, Sandboden 3,2 %, Schwarzerde 3,0 %, Salzboden 2,7 %, Vulkanboden 1,6 %, Rest unter 1,5 %. Fruchtbarkeit im Mittel 2,47 (Fläche gewichtet), 16 % des Landes ab 3,5.

Verlauf der Abstimmung: Die erste Tabelle folgte der Erde, wo warme Böden arm sind (Fruchtbarkeit 2,28). Danach wurde die Welt fruchtbarer gemacht (Cfa Braunerde, BSh Kastanienerde statt Kalkkruste, Vertisol in Aw/As überall, breitere Auen; 2,61, Braunerde 28,8 %). Im Vergleich mit groben Erdwerten waren dann Vulkanboden (6,5 %) zu häufig, Fels (2,0 %), Sandboden (0,1 %) und Schwarzerde (0,3 %) zu selten und Braunerde zu häufig; daraus die Regeln für junge Vulkangebiete, alten Basalt, Sandmeere, Hochgebirge, feuchtes Cfa und den breiteren Schwarzerde-Gürtel. Braunerde und Kastanienerde bleiben bewusst über den Erdwerten (etwa 12 % und 4 %), Gley, Moor, Podsol und Permafrost darunter, weil die Welt wärmer ist (D-Klimate 14 %, E-Klimate 1 %).

- TODO: Löss braucht die Windrichtung (T-039). Bis dahin entfällt die Löss-Regel, der Layer `loessboden` ist leer; einen Ersatzweg gibt es nicht.
- O - Salzböden an Enden von Trockenflüssen: Nach Abschnitt 7 laufen alle Senken über (O - Endseen). Bis das entschieden ist, kommen Salzböden nur aus Klima, Küste und aufgefüllten Senken in B-Klimaten.
- O - Kiesanteil entlang von Trockenflüssen (`materialien_und_boeden.md` 6.3) ist kein Boden; gehört zu den Lagerstätten (T-082).
- O - Prüfen: große Salzbodenflächen um die Binnengewässer in der Mitte des Ostkontinents (Senken in BW/BS); die Vulkanboden-Fläche im Westen des Ostkontinents nördlich des Äquators ist Gesteinsraum `HOTSPOT` auf dem Festland.

---

## 12. Pflanzen

Liste und Angaben je Pflanze: `lore/pflanzen.md`.

**Eignung je Zelle:**

```
eignung = K(köppen) × B(boden) × W(wasser) × H(höhe) × S(salz) × rauschen(startwert)
```

- **K:** Gewicht der Köppen-Klasse (Kernzone 1,0, Randzone 0,5). Randzone = angrenzende Klasse innerhalb eines Streifens (O - Breite in km).
- **B:** bevorzugter Boden 1,0, tolerierter 0,5, ausgeschlossener 0, gewichtet nach den Bodenanteilen der Zelle; dazu der pH-Abgleich.
- **W:** passende Wasserkategorie 1,0, sonst 0 oder abgeschwächt.
- **H:** nur bei angegebenem Höhenbereich; die meisten Höhenwirkungen stecken schon in der Köppen-Klasse.
- **S:** Salzboden ohne Salztoleranz → 0.
- **Rauschen:** niederfrequent, damit Pflanzen fleckig wachsen.

**Linien:** Landmassen sind zusammenhängende Landflächen; kleine Inseln gehören zur nächsten großen Landmasse, wenn sie nahe genug liegen (O - Abstand). Pflanzen mit Linie bekommen je Landmasse einen eigenen Layer (`<id>_<landmasse>`).

**Prüfungen:** Warnung für jede Pflanze ohne Vorkommen und für jede Landzelle außer Eis ohne Pflanze. Abdeckung der Grundbedürfnisse (Nahrung, Holz oder Brennmaterial, Faser) je Zelle; fehlende Kombinationen werden geloggt, nicht aufgefüllt (Zielwert, Design-Referenz Abschnitt 4).

**Ausgabe:** `data/world/pflanzen.bin` (Kennung `TBPF`), Bild `lore/pflanzen_vielfalt.png` (Anzahl Pflanzen je Zelle), Einzelbilder unter `lore/ebenen/pflanzen/`.

- O - Konkurrenz innerhalb einer Wuchsform (Bäume einer Zelle teilen sich den Platz). Ohne sie lässt sich „Schatten“ nicht abbilden.
