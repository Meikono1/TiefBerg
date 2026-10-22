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

**Quelle:** `02-climate_zones-raw.png`, auf 2160 × 1080 verkleinert. Land wird über die Meeresmaske bestimmt, jedes Pixel bekommt die nächstliegende Köppen-Legendenfarbe (Legendenfarben × 255/243).

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
5. **Meer:** Ordinalwerte geglättet (Gauß 3 × 5 px)
6. **Riff:** tropisches Meer oder gemäßigtes Meer mit |Breite| < 28°, höchstens 10 px (≈ 1,7°) vor der Küste

---

## 6. Völker und Kulturen (um das Jahr 1000)

### Prinzip

- **Einfluss je Volk:** `I = clip(Eignung · exp(−(d / R)²) · 1,25; 0; 1)`, wobei d die Distanz über Land ist (kein Weg übers Meer). Rechenraster 1440 × 720.
- **Eignung:** aus dem Lebensraum-Profil, geglättet (Gauß 2), mit optionalem Bonus:
  - Fluss: `× (0,55 + 0,6 · Flussnähe)`
  - Küste: `× (0,55 + 0,6 · Küstennähe)`, Küstennähe innerhalb von 3°
- **Kultur:** `1 − Π(1 − I)` über ihre Völker.
- **Kerngebiet:** Einfluss > **0,50** (kräftige Farbe mit Umriss).
- **Einflussgebiet:** Einfluss > **0,22** (blasse Farbe).
- **Mischgebiet:** Die zweitstärkste Kultur liegt über 0,22 und über 45 % der stärksten → diagonale Streifen, Periode 14 px (auf 4320).
- Alles darunter ist Wildnis (ca. 47 % des Landes).

### Lebensraum-Profile (Zonen-ID → Eignung)

| Profil | Zonen |
|---|---|
| ACKER | 6: 1, 7: 1, 8: 1, 9: 0,8, 5: 0,5, 2: 0,5, 1: 0,3, 10: 0,3, 3: 0,1, 13: 0,15 |
| WALD | 8: 1, 7: 0,9, 9: 0,8, 10: 0,7, 1: 0,6, 6: 0,5, 2: 0,3, 5: 0,2, 13: 0,2 |
| GEBIRGE | 13: 1, 9: 0,55, 10: 0,5, 8: 0,5, 5: 0,45, 6: 0,45, 7: 0,4, 11: 0,3, 2: 0,3, 3: 0,2 |
| WÜSTE | 3: 1, 4: 0,9, 5: 0,8, 2: 0,4, 6: 0,3 |
| STEPPE | 5: 1, 2: 0,9, 6: 0,6, 9: 0,6, 3: 0,45, 4: 0,45, 7: 0,3 |
| SAVANNE | 2: 1, 5: 0,8, 1: 0,45, 7: 0,5, 3: 0,3 |
| DSCHUNGEL | 1: 1, 2: 0,6, 7: 0,75 |
| KÄLTE | 10: 1, 9: 0,8, 11: 0,9, 12: 0,2, 13: 0,4, 8: 0,5 |

Kombinationen wie „Wald + Gebirge“ nehmen je Zone den höheren Wert.

### Kulturen und Völker

Format: **Volk (Länge, Breite, Radius °, Profil, Bonus)**

| ID | Kultur | Farbe (RGB) | Völker |
|---|---|---|---|
| 1 | HUMANIC | 225, 60, 60 | Human (−102, 45, 13, Acker, Fluss); Elf (−84, 42, 9, Wald); Dwarf (−139, 41, 8, Gebirge); Gnome (−126, 45, 8, Wald + Acker + Gebirge) |
| 2 | FEYLIN | 245, 130, 200 | Halfling (8, 45, 7, Acker, Fluss); Changeling (15, 50, 7, Wald); Pixie (−12, 60, 4, Wald) |
| 3 | FENNARI | 80, 150, 235 | Wolfkin (12, 63, 9, Kälte); Husky (−112, 67, 8, Kälte, Küste); Shepherd (32, 52, 7, Steppe + Acker); Kitsune (155, 36, 5, Wald); Fennec (4, 22, 7, Wüste) |
| 4 | TARPAN | 235, 165, 50 | Equus (−95, 10, 12, Steppe); Asinus (−108, −14, 9, Wüste + Steppe); Hippotigris (−100, −3, 9, Savanne) |
| 5 | RUMINAN *(vorläufig)* | 200, 205, 90 | Bovinea (−88, 24, 10, Steppe + Acker, Fluss); Cervinae (−98, 32, 9, Wald); Capreolinae (−82, 32, 8, Wald); Saola (−130, 0, 5, Dschungel + Gebirge) |
| 6 | RODINI | 160, 105, 60 | Chinchilla (−114, −34, 6, Gebirge); Caviid (−102, −30, 8, Acker + Savanne, Fluss); Leporid (−104, −42, 7, Steppe); Squirrel (−98, −40, 6, Wald); Mustelid (−101, −50, 5, Kälte + Wald, Küste); Murid (−110, −25, 12, Acker, Fluss); Jerboa (−120, 34, 6, Wüste); Euplerid (140, −22, 6, Dschungel + Wald) |
| 7 | VESKRAL | 140, 70, 175 | Girtablilu (−126, 32, 8, Wüste); Araneae (−120, 14, 8, Dschungel + Wald); Mantodea (−112, 22, 8, Savanne + Steppe); Odonata (−122, 6, 7, Dschungel, Fluss) |
| 8 | NAGALITH | 30, 165, 105 | Reptilid (34, 0, 9, Dschungel, Fluss); Serathi (42, −6, 8, Dschungel); DraconicNaga (50, 13, 7, Gebirge); Lamia (60, 14, 8, Wüste + Steppe); Viper (66, 6, 8, Savanne + Wüste) |
| 9 | KATARU | 250, 205, 40 | Pantherine (56, −3, 8, Dschungel + Savanne); Serval (66, −9, 7, Savanne, Fluss); Acinonyx (76, −13, 9, Savanne); Caracal (84, −18, 8, Steppe + Wüste) |
| 10 | HYRAX | 205, 95, 40 | Gnoll (52, −21, 9, Steppe + Savanne); Jackal (43, −18, 8, Wüste, Fluss); Bat (60, −17, 6, Gebirge + Savanne); Cristasus *(Lebensraum offen)* (48, −27, 7, Steppe) |
| 11 | GOBLIN | 125, 190, 40 | Goblin (110, −12, 9, Wald + Dschungel); Hobgoblin (121, −18, 7, Wald + Gebirge) |
| 12 | DRACONIK | 235, 85, 25 | Dragonkin (58, 33, 6, Gebirge); Kobold (46, 30, 8, Gebirge + Steppe); Felbold (72, 29, 8, Gebirge + Steppe) |
| 13 | TENGU | 110, 215, 225 | Gryphon (106, 36, 7, Gebirge); Owl (124, 35, 8, Wald + Gebirge); Harpy (140, 26, 6, Gebirge, Küste); Falcon (96, 42, 8, Steppe) |
| 14 | BRUTAHK | 165, 165, 190 | Giant (46, −46, 8, Kälte + Gebirge); Goliath (78, −46, 8, Gebirge); Ursine (62, −49, 8, Wald + Kälte) |
| 15 | CORALOR | 40, 205, 195 | Land: Crocodilian (162, 1, 6, Dschungel, Fluss); Axolotl (69, −12, 3, Gebirge + Savanne). Meer (nur Beschriftung): Hippocampus (158, −7), Cephalopod (172, 4), Carcharhini (149, 6), Cetacean (176, −13), Pinniped (−94, −56) |
| – | MERMAN | 120, 160, 255 | Nur Beschriftung im offenen Meer: Merman (−35, 21), Leviathan (−160, −28), Siren (−58, 17), Delphinoid (−28, −12) |

**CORALOR-Flachmeer:** 2,5° breiter Streifen um den Tropen-Archipel (Länge > 145°, Breite −14° bis 15°) und die Großinsel.

**Freies Land** (bewusst unbesiedelt): Südland, Doppelinsel, Eisschild, Vulkaninsel, Hotspot- und Karibik-Inseln.
