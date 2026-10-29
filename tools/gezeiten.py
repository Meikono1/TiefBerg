"""Tidenhub des Meeres als Raster (T-085); Grundlage der Wasserkategorie GEZEITEN in tools/bodenfeuchte.py.

Regeln: lore/welt_konfiguration.md Abschnitt 11 (Gezeiten).
Quellen:
  data/world/klimazonen.bin            Land, Meer, Binnengewässer
  lore/world/tides/tides-raw.png       Gezeitenbild der Klimasimulation, Lauf 2026-10-23, 8192 × 4096
                                       (Legende in tides-framed.png: 0 m … 46 cm … 1,7 m+, Linien alle 5 cm)
Die Klimasimulation gibt die Gezeiten nur als Bild aus. Der Tidenhub wird über die Farbskala der Legende zurückgerechnet,
die weißen Höhenlinien werden aus den Nachbarpixeln aufgefüllt.
Ergebnis:
  data/world/gezeiten.bin              Einzelraster: Tidenhub in cm je Meereszelle (0–170, 170 = 1,7 m und mehr),
                                       Land und Binnengewässer 0
  lore/gezeiten.png                    Tidenhub über dem Meer, Land grau

Aufruf aus dem Projektordner:  python tools/gezeiten.py
Benötigt numpy, Pillow, scipy.
"""

import logging
import sys
import time

import numpy as np
from PIL import Image
from scipy import ndimage

import ebenen

log = logging.getLogger("gezeiten")

SRC_TIDES = ebenen.ROOT / "lore" / "world" / "tides" / "tides-raw.png"
OUT_NAME = "gezeiten.bin"
OUT_MAGIC = b"TBGZ"
OUT_IMAGE = ebenen.ROOT / "lore" / "gezeiten.png"

# Farbstufen der Legende (aus tides-framed.png abgelesen) und ihr Tidenhub in cm. Die Legende beschriftet nur 0, 46 cm
# (vierte Stufe) und 1,7 m+ (letzte). Annahme (O - Vorschlag): Die Stufen liegen je Hälfte gleichmäßig. Für die untere
# Hälfte bestätigen die 5-cm-Höhenlinien das deutlich. Für die obere spricht der Abgleich mit dem Log der Simulation:
# Zellmittel des Meeres 53 cm (Simulation: „Ozeanmittel 52 cm“), Median 45 cm (Legendenmitte 46 cm).
RAMP_COLORS = np.array([(33, 44, 66), (42, 81, 123), (50, 105, 157), (56, 124, 184),
                        (117, 168, 211), (152, 201, 234), (179, 229, 255)], np.float32)
RAMP_CM = np.array([0.0, 46 / 3, 92 / 3, 46.0, 46 + 124 / 3, 46 + 248 / 3, 170.0])
MAX_CM = 170                  # über 1,7 m zeigt das Bild keinen Unterschied mehr
RAMP_TOLERANCE = 6.0          # Farbabstand, bis zu dem ein Pixel als Farbskala gilt (sonst Land oder Höhenlinie)
SAMPLE_STEP = 2               # jedes zweite Pixel reicht für 2160 × 1080 und spart Speicher
LINE_MARGIN = 1               # Pixel um Land und Höhenlinien, die ebenfalls aufgefüllt werden
# Die geglätteten Ränder der Linien sind Blau mit Weiß gemischt und liegen damit auf der Skala (zu hoch). Ein Median über
# ein Fenster, das breiter ist als eine Linie, nimmt sie heraus.
LINE_MEDIAN = 7
LAND_COLOR = (95, 95, 95)
LEGEND_STEPS_CM = [0, 25, 50, 75, 100, 125, 150, 170]


def ramp_position(pixels):
    """Lage jedes Pixels auf der Farbskala (0 … Anzahl Stufen − 1) und sein Farbabstand zur Skala."""
    best = np.full(len(pixels), np.inf, np.float32)
    position = np.zeros(len(pixels), np.float32)
    for i in range(len(RAMP_COLORS) - 1):
        start, step = RAMP_COLORS[i], RAMP_COLORS[i + 1] - RAMP_COLORS[i]
        t = np.clip(((pixels - start) @ step) / (step @ step), 0.0, 1.0)
        distance = np.linalg.norm(pixels - (start + t[:, None] * step), axis=1)
        closer = distance < best
        best[closer] = distance[closer]
        position[closer] = i + t[closer]
    return position, best


def read_tides(width, height):
    """Tidenhub in cm aus dem Bild, aufs Zielraster gebracht; Land und Höhenlinien aus der Nachbarschaft aufgefüllt."""
    if not SRC_TIDES.is_file():
        raise FileNotFoundError(f"Quelle fehlt: {SRC_TIDES}")
    image = np.array(Image.open(SRC_TIDES).convert("RGB"))[::SAMPLE_STEP, ::SAMPLE_STEP]
    rows, cols = image.shape[:2]
    position, distance = ramp_position(image.reshape(-1, 3).astype(np.float32))
    # Die Höhenlinien sind geglättet; ihre Randpixel liegen noch nah an der Skala, sind aber zu hell.
    valid = ndimage.binary_erosion((distance <= RAMP_TOLERANCE).reshape(rows, cols), iterations=LINE_MARGIN)
    if not valid.any():
        raise ValueError(f"{SRC_TIDES.name}: kein Pixel passt zur Farbskala der Legende")
    log.info("Pixel auf der Farbskala: %.1f %% (Rest Land und Höhenlinien)", valid.mean() * 100.0)
    tides = np.interp(position, np.arange(len(RAMP_CM)), RAMP_CM).reshape(rows, cols).astype(np.float32)
    nearest = ndimage.distance_transform_edt(~valid, return_distances=False, return_indices=True)
    tides = ndimage.median_filter(tides[tuple(nearest)], size=LINE_MEDIAN, mode="nearest")
    return np.array(Image.fromarray(tides, mode="F").resize((width, height), Image.BILINEAR))


def build():
    zones = ebenen.read_raster("klimazonen.bin", b"TBKZ")
    height, width = zones.shape
    ocean = np.isin(zones, list(ebenen.WATER_ZONES - {ebenen.INLAND_WATER_ZONE}))
    tides = np.where(ocean, np.clip(np.rint(read_tides(width, height)), 0, MAX_CM), 0).astype(np.uint8)
    land = ~ocean & (zones != ebenen.INLAND_WATER_ZONE)
    area = ebenen.cell_area_km2(height, width)
    coast = ocean & (ebenen.wrapped_distance_km(land) <= ebenen.KM_PER_DEG * 180.0 / height * 1.5)
    log.info("Meer: Tidenhub im Flächenmittel %.0f cm, 1,7 m und mehr auf %.1f %%",
             float((tides * area)[ocean].sum() / area[ocean].sum()),
             float(area[ocean & (tides >= MAX_CM)].sum() / area[ocean].sum() * 100.0))
    for limit in (50, 100, MAX_CM):
        log.info("Küstennahes Meer: Tidenhub ab %d cm auf %.1f %%", limit,
                 float(area[coast & (tides >= limit)].sum() / area[coast].sum() * 100.0))
    return tides, land, ocean


def write_raster(values):
    """Einzelraster nach ebenen.py: Kennung, uint32 Version, Breite, Höhe, ein Byte je Zelle."""
    height, width = values.shape
    path = ebenen.WORLD_DIR / OUT_NAME
    ebenen.WORLD_DIR.mkdir(parents=True, exist_ok=True)
    with open(path, "wb") as file:
        file.write(OUT_MAGIC)
        file.write(np.array([ebenen.FORMAT_VERSION, width, height], "<u4").tobytes())
        file.write(np.ascontiguousarray(values, np.uint8).tobytes())
    log.info("Raster geschrieben: %s (%d Bytes)", path, path.stat().st_size)


def ramp_color(cm):
    position = np.interp(cm, RAMP_CM, np.arange(len(RAMP_CM)))
    return np.stack([np.interp(position, np.arange(len(RAMP_CM)), RAMP_COLORS[:, c]) for c in range(3)], axis=-1)


def write_image(tides, land, ocean):
    image = np.where(ocean[..., None], ramp_color(tides.astype(np.float32)), np.array(ebenen.SEA_COLOR, np.float32))
    image = np.where(land[..., None], np.array(LAND_COLOR, np.float32), image)
    entries = [(tuple(ramp_color(np.float32(cm)).astype(int)), f"{cm} cm" + (" und mehr" if cm == MAX_CM else ""))
               for cm in LEGEND_STEPS_CM]
    entries.append((ebenen.SEA_COLOR, "Binnengewässer"))
    ebenen.with_legend(np.rint(image).astype(np.uint8), "Tidenhub", entries).save(OUT_IMAGE)
    log.info("Bild geschrieben: %s", OUT_IMAGE)


def main():
    sys.stderr.reconfigure(encoding="utf-8")
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(funcName)s: %(message)s")
    started = time.perf_counter()
    log.info("Start Gezeiten")
    try:
        tides, land, ocean = build()
        write_raster(tides)
        write_image(tides, land, ocean)
    except Exception:
        log.exception("Gezeiten konnten nicht erzeugt werden")
        return 1
    log.info("Fertig in %.1f s", time.perf_counter() - started)
    return 0


if __name__ == "__main__":
    sys.exit(main())
