# Klima, Siedlung und Handel

Auswertung der World-Climate-Lab-Simulation (Version 0.1.10, Export vom 20.10.2026).
Karten im selben Ordner: `tiefberg_siedlung.jpg` (Siedlungspotenzial), `tiefberg_handel.jpg` (Wohlstand und Handel), `tiefberg_segelkarte.jpg` (Prüfrouten), `wind_vorschau_mXX.jpg` (Wind je Monat).
Spieldaten zur Seefahrt: `data/world/`, Format in `defines/Seewind Datenformat.md`.
Rohdaten aus World Climate Lab (alle Ebenen, Klimadaten, Aufnahmen Wind- und Meeresströmung als Einzelbilder und GIF): `lore/world/`. Der Ordner hat eine `.gdignore`, Godot importiert ihn nicht.

Status-Präfixe wie in der Design-Referenz: `E -` entschieden, `O -` offen, `O - Vorschlag:` darf übernommen werden.

## Datengrundlage

- Raster 2048×1024, Längen-Breiten-Projektion, −180° bis 180°, erste Zeile = Norden.
- Deckt sich mit der aktuellen Weltkarte (Eis nach dieser Atmosphäre): Die Landflächen stimmen zu 93 % überein. Kleine Abweichungen gibt es an Küsten und Meerengen.
- Werte kommen aus den 16-Bit-Klimadaten: Temperatur (Jahr, kältester und wärmster Monat), Niederschlag (Jahr, Monat 1 und 7), Höhe, Köppen-Klasse. Flüsse stammen aus der Ebene `06-rivers`.
- Der Export enthält **keine Windrichtung**, nur die Windstärke. Die Richtung ist abgeleitet, siehe unten.

## Wind

**Richtung gemessen** aus der WCL-Aufnahme „Windzirkulation“ (96 Bilder, 8 je Monat). Böen und Starkwindflecken wandern von Bild zu Bild. Ihre Zugrichtung wird über viele Bildpaare gemittelt, getrennt nach Jahreszeiten (Skript `flow`, Kreuzkorrelation).

- **Ostwärts** fast überall zwischen ~55°N und ~50°S, am stärksten in den Starkwindbändern um 30–40°N und 30–40°S. Das deckt sich mit der Beobachtung im GIF: „von Westen nach Osten, dann nach unten“.
- **Nord-Süd-Anteil:** Im schmalen Ozean zwischen den Kontinenten dreht der Wind bei 30–40°N nach Süden. Im Süden zieht er in der ersten Jahreshälfte stark nach Norden.
- **Westwärts** nur in hohen Breiten (~60°S, teils ~55–60°N).
- **Jahreszeit:** Die Bänder wandern. Das Nordband liegt in den Monaten 1–6 bei ~38–39°N, in den Monaten 8–9 bei ~30°N und ist dann am stärksten. Das Südband liegt in den Monaten 2–4 bei ~30°S und ist dann am stärksten, in den Monaten 10–11 bei ~40°S.

O - Vorschlag: Die Zugrichtung der Böen wird als Windrichtung genommen. Der Nord-Süd-Anteil ist unsicherer als die Ost-West-Richtung, weil auch das jahreszeitliche Wandern der Bänder als Bewegung erscheinen kann.

Die frühere Annahme, dass die Starkwindbänder nach Westen wehen, ist damit **ersetzt**. Ihr Beleg war ein Regenvergleich der Küsten.

## Klima an Land (flächengewichtet)

BSh 23 %, BWh 16 %, Aw 10 %, Cwa 8 %, Cfa 7 %, Dfb 7 %, As 4 %, Csa 4 %, BSk 3 %, Dfc 3 %, Cfb 3 %, ET 3 %.

- Rund **40 % des Landes sind trocken** (Wüste oder Steppe).
- Rund **ein Drittel** hat eine starke Regenzeit: Der Monat 1 oder 7 bringt mehr als das Vierfache des anderen.
- **Regenwald (Af/Am):** Die Siedlungsdichte ist dort gering, wegen armer Böden, Krankheiten und schwerer Rodung. Im Modell ist die Fruchtbarkeit dort gesenkt (Af ×0,45, Am ×0,7). Die Flächen stammen aus dem Export und aus dem neueren Klimazonen-Bild des Entwicklers. Betroffen sind die Mitte des Westkontinents, der Westen des Ostkontinents um den Golfkopf und die Inseln im Südosten.

## Kernräume (dicht besiedelbar)

1. **Kettenküste:** fruchtbarer Streifen an der Gebirgsbrücke im Nordosten, Klima Cfa. Wird wahrscheinlich das wirtschaftliche Zentrum.
2. **Tropische Kornkammer:** Südosten des Ostkontinents, Seen und Flüsse, Klima Cwa mit Monsun. Größte Bevölkerung.
3. **Nordseenland:** Norden des Westkontinents, Klima Dfb, viele Flüsse, kühl.
4. **Stromland:** Mitte des Westkontinents, Klima Cwa.
5. **Südostinsel:** Klima Cfa, eher abgelegen.
6. **Nordhalbinsel:** Klima Cfb.

## Handel

Grobe Einschätzung, wo sich Wohlstand sammelt. Güter kommen später. Grundlage sind:

- Jeder Naturraum hat eigene Pflanzen und Tiere. Handel entsteht zwischen **verschiedenen** Naturräumen.
- Fahrzeiten aus den Seewind-Daten: Kogge, Mittel aus Abfahrt im Monat 1 und im Monat 7, Maßstab 5 km pro Pixel.
- Bevölkerung des Hinterlands aus der Fruchtbarkeit (Regenwald abgewertet).

O - Vorschlag: Das Handelspotenzial zweier Häfen ist Unterschied der Naturräume × e^(−Fahrtage/45) × √(Bevölkerung A × Bevölkerung B). Wohlstand ist Bevölkerung plus Summe der Handelspotenziale.

| Rolle | Bedeutung |
|---|---|
| Drehscheibe | großes Hinterland und viel Handel, Wohlstand sammelt sich |
| Export | Naturraum, der bei den erreichbaren Partnern selten ist |
| Import | wenig fruchtbares Hinterland oder kleine Bevölkerung, braucht Zufuhr |
| Regional | handelt vor allem mit ähnlichen Nachbarn |

**Wichtigste Einschätzungen:**

- Am **Isthmus** liegt keine Meerenge, sondern Land. Der Kanal ist O - spätere Technologie, mit `navgrid.bin` Code 2 vorgesehen.
- Die Gebirgsbrücke im Nordosten hat viele Häfen mit ähnlichem Naturraum. Viel Küstenhandel, aber wenig Ergänzung. Hafen 7 ist die Drehscheibe.
- Der Südosten des Ostkontinents (2 und 8) verbindet Savanne, Wüstenrand und Kontinentales Klima. Hier ist das stärkste Handelsnetz auf kurzer Distanz.
- Der Westkontinent hat zwei Pole: den Nordwesten (1 und 6, Gemäßigt und Mediterran) und das Südkap (4, Mediterran).
- Die Südküste des Ostkontinents (11, 15, 20, 30) exportiert, weil ihre Naturräume im Norden fehlen.
- Die längste regelmäßige Verbindung ist 25 ↔ 5 über den schmalen Ozean.

### Häfen

Die Positionen sind Pixel der Karte 4320×2160 und nach Wohlstand sortiert. Namen vergibst du.

| # | Rolle | Naturraum Hinterland | Pixel x, y |
|---|---|---|---|
| 1 | Drehscheibe | Gemäßigt | 419, 571 |
| 2 | Drehscheibe | Kontinental | 3088, 1670 |
| 3 | Drehscheibe | Gemäßigt | 2569, 537 |
| 4 | Drehscheibe | Mediterran | 776, 1466 |
| 5 | Drehscheibe | Gemäßigt | 2265, 596 |
| 6 | Drehscheibe | Mediterran | 451, 683 |
| 7 | Drehscheibe | Kontinental | 3280, 499 |
| 8 | Drehscheibe | Savanne | 3547, 1126 |
| 9 | Regional | Feucht-subtropisch | 3396, 1394 |
| 10 | Regional | Feucht-subtropisch | 3233, 656 |
| 11 | Export | Kontinental | 2780, 1662 |
| 12 | Regional | Feucht-subtropisch | 3866, 769 |
| 13 | Regional | Feucht-subtropisch | 3801, 666 |
| 14 | Regional | Kontinental | 3404, 605 |
| 15 | Export | Gemäßigt | 2501, 1565 |
| 16 | Regional | Feucht-subtropisch | 3522, 1417 |
| 17 | Regional | Feucht-subtropisch | 3010, 664 |
| 18 | Regional | Feucht-subtropisch | 3630, 1282 |
| 19 | Regional | Kontinental | 2343, 504 |
| 20 | Export | Subarktisch/Tundra | 2915, 1691 |
| 21 | Regional | Feucht-subtropisch | 4203, 1425 |
| 22 | Regional | Feucht-subtropisch | 890, 1554 |
| 23 | Regional | Feucht-subtropisch | 3621, 765 |
| 24 | Regional | Feucht-subtropisch | 913, 1299 |
| 25 | Import | Kontinental | 1267, 592 |
| 26 | Regional | Feucht-subtropisch | 3375, 742 |
| 27 | Export | Steppe | 799, 656 |
| 28 | Export | Steppe | 2227, 1198 |
| 29 | Import | Kontinental | 2219, 417 |
| 30 | Export | Feucht-subtropisch | 2238, 1567 |
| 31 | Import | Feucht-subtropisch | 664, 1364 |
| 32 | Import | Feucht-subtropisch | 1016, 1453 |
| 33 | Regional | Feucht-subtropisch | 3501, 759 |
| 34 | Export | Steppe | 2963, 795 |

## Seefahrt

Daten für den Router: Wasser/Land-Raster und Wind je Monat, 1080×540, 20 km pro Zelle. Die Windstärke je Monat und die Lage der Starkwindbänder stammen aus der WCL-Aufnahme „Windzirkulation“ (96 Bilder, 8 je Monat). Die Richtungen kommen aus der Zugrichtung der Böen in derselben Aufnahme, siehe Abschnitt Wind. Schiffstypen stehen in `data/ships/ship_types.yaml`. Format und Algorithmus beschreibt `defines/Seewind Datenformat.md`.

Prüfrouten der Kogge (`tiefberg_segelkarte.jpg`), jeweils hin und zurück:

| Strecke | Abfahrt Monat 1 | Abfahrt Monat 7 |
|---|---|---|
| Binnenmeer-Tor ↔ Monsunhafen | 49 / 26 Tage | 27 / 45 Tage |
| Südkap ↔ Westkap | 23 / 24 Tage | 20 / 50 Tage |
| Nordbucht ↔ Südkap | 53 / 36 Tage | 30 / 59 Tage |

- Hin und zurück laufen oft auf verschiedenen Wegen.
- Die Jahreszeit entscheidet: Dieselbe Strecke dauert zur falschen Zeit oft fast doppelt so lange. Binnenmeer-Tor → Monsunhafen braucht im Monat 1 49 Tage, im Monat 7 27 Tage. Die Rückfahrt ist genau umgekehrt.

## Probleme und Konfliktpotenzial

- **Trockenzonen:** Süden des Ostkontinents, die Westkap-Halbinsel und der Westen des Westkontinents. Siedlung ist dort nur an Flüssen möglich.
- **Monsun:** Ausbleibender Regen bringt Hungersnöte, zu starker Regen Überschwemmungen. Das betrifft vor allem die Kornkammer.
- **Gebirgsbrücke:** Über Land kommt man nur über Pässe. Wer sie hält, kontrolliert den Landhandel zwischen Ost und West.
- **Meereis:** Im Norden gibt es Meereis ab ~70°N, das ganze Jahr ab ~80°N. Im Süden gibt es Meereis erst ab ~70°S und nur im Winter. Die Handelsrouten sind davon nicht betroffen.
- **Wenige Nadelöhre:** Isthmus (Landweg, später Kanal) und Binnenmeer-Tor ziehen Reichtum an und damit auch Kriege.

## Offen

- O - Drehrichtung und Achsneigung des Planeten aus World Climate Lab übernehmen.
- O - Windrichtung ist aus der Zugrichtung der Böen gemessen, nicht exportiert. Falls WCL später Windvektoren exportiert, die Monatsdateien damit neu erzeugen.
- O - Ob die Klimadaten (Köppen, Niederschlag, Temperatur) als Spieldaten für die Simulation genutzt werden. Siehe Design-Referenz, Abschnitt Welt.
