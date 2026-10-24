# Verbreitung der Völker – Heimatgebiete

Jedes Volk hat einen Mittelpunkt, einen Radius und ein Lebensraum-Profil. Daraus wird berechnet, wo seine Heimat liegt: Nahe am Mittelpunkt und in passenden Klimazonen ist sein Einfluss stark, mit wachsender Entfernung und in ungeeignetem Land verblasst er. Wo der Einfluss hoch ist, im Kerngebiet, liegt das Heimatgebiet. Dort wählt die Vorsimulation zufällig die Punkte für die ersten Städte, und von dort wächst das Volk in tausend Jahren über seine Heimat hinaus. Einfluss- und Mischgebiet der Berechnung zeigen, wie weit es dabei ungefähr kommen soll (Design-Referenz Abschnitt 4).

Das Profil beschreibt das natürliche Habitat, nicht die Grenze dessen, was ein Volk kann. Ein Gebirgsvolk gründet auch im Flachland Städte, wenn es dorthin zieht. Die Völker einer Kulturgruppe liegen bewusst nahe beieinander. Wo sich ihre Heimatgebiete überlappen, leben sie nebeneinander; ob daraus gemeinsame oder eigene Städte werden, entscheidet die Vorsimulation, nicht die Karte.

Die Werte stehen als Daten in `data/peoples/lebensraum.yaml` (Profile) und `data/peoples/kulturen.yaml` (Kulturgruppen und Völker mit Mittelpunkt, Radius, Profilen und Bonus). Diese Datei beschreibt das Verfahren. Das gemalte Bild aus T-043 legt sich später über die berechneten Gebiete und hat dort Vorrang, wo es bemalt ist.

## Berechnung

Gerechnet wird beim Weltstart (`HomelandCalculator` im Simulationskern) auf einem Raster von 1440 × 720 Zellen über den Klimazonen und Flüssen (`lore/welt_konfiguration.md` Abschnitt 5 und 7). Die Völker werden parallel gerechnet; das Ergebnis ist immer gleich. Dauer rund 1,2 s.

- **Einfluss je Volk:** `I = clip(Eignung · exp(−(d / R)²) · 1,25; 0; 1)`. d ist der kürzeste Weg über Land vom Mittelpunkt (kein Weg übers Meer oder über Binnengewässer). Annahme: Ost-West-Schritte zählen mit dem Kosinus der Breite, ein Grad Länge ist im Norden also kürzer als am Äquator. Jenseits von 2,2 R wird nicht mehr gerechnet, dort bleibt der Einfluss unter 1 %.
- **Eignung:** je Zone der höchste Wert der Profile des Volks, Meer und Binnengewässer 0, geglättet (Gauß, Sigma 2 Zellen), mit optionalem Bonus:
  - Fluss: `× (0,55 + 0,6 · Flussnähe)`. Annahme: Flussnähe fällt von 1 am Fluss linear auf 0 in 1° Abstand; nur ganzjährige Flüsse zählen, Trockenflüsse nicht.
  - Küste: `× (0,55 + 0,6 · Küstennähe)`, Küstennähe fällt linear auf 0 in 3° Abstand zum Meer.
- **Kultur:** `1 − Π(1 − I)` über ihre Völker.
- **Kerngebiet:** stärkste Kultur über **0,50**.
- **Mischgebiet:** sonst, wenn die zweitstärkste Kultur über 0,22 und über 45 % der stärksten liegt.
- **Einflussgebiet:** sonst, wenn die stärkste Kultur über **0,22** liegt.
- Alles darunter ist Wildnis.
- **Heimatgebiet eines Volks:** Kerngebiet seiner Kultur, in dem sein eigener Einfluss über 0,22 liegt (Annahme). Benachbarte Völker einer Kultur überlappen dort.

Ergebnis (2026-10-23, ohne CORALOR, Saola bei −98 / 13), Anteil am Land: Kern 32,2 %, Einfluss 18,1 %, Misch 1,6 %, Wildnis 48,1 %.

## Meeresvölker

Sie haben noch kein berechnetes Gebiet und stehen nicht in den Daten, nur als Beschriftung im Schaubild (Länge, Breite):

- **MERMAN** (Farbe 120, 160, 255): Mermaid (−35, 21), Siren (−58, 17; Delphinoid bei −28, −12 ist in Siren aufgegangen), Undine und Cecaelia noch ohne Mittelpunkt; Leviathan (−160, −28) als mystische Wesen (`lore/voelker/merman.md`)

**Freies Land** (bewusst unbesiedelt): Südland, Doppelinsel, Eisschild, Vulkaninsel, Hotspot- und Karibik-Inseln. Es ist nicht als Fläche festgelegt; die Berechnung lässt es leer, weil kein Weg über Land dorthin führt.

## Offen

- O - Meeresvölker: wie sie zu einem Gebiet kommen.
- O - Vier Mittelpunkte liegen auf der heutigen Karte im Wasser, diese Völker haben kein Heimatgebiet: Gnome (−126, 45; nächstes Land −119,4 / 44,9), Husky (−112, 67; −113,8 / 66,9), Girtablilu (−126, 32; −125,6 / 31,8) im Meer, Jackal (43, −18; 44,2 / −19,2) in einem Binnengewässer. CORALOR ist vorerst gestrichen; das Schaubild `voelkerkarte.png` zeigt sie noch. Das Schaubild `voelkerkarte.png` stammt aus einem älteren Stand der Küsten im Nordwesten.
