"""Berechnet die Flussebene: Hauptflüsse aus Höhe und Niederschlag (T-069).

Regeln: lore/welt_konfiguration.md, Abschnitt 7. Quellen (Abschnitt 6):
  lore/Version 5 HM.png                                        Heightmap, Grauwert × 8849 m
  data/world/klimazonen.bin                                    Land, Ozean und Binnengewässer (tools/klimazonen.py)
  lore/world/climate-data/precipitation_annual                 Niederschlag mm/Jahr (Lauf 2026-10-23)
  lore/world/climate-data/temperature_annual_mean              Temperatur °C (für die Verdunstung)
  lore/flussvorgaben.yaml                                      von Hand vorgegebene Flussläufe
Vergleich (nur Vorschau): lore/world/all-layers/06-rivers-raw.png (Lauf 2026-10-10)
Ergebnis:
  data/world/fluesse.bin    Kopf + ein Byte Größenklasse je Zelle (0 = kein Fluss), Raster der Klimazonen
  data/world/fluesse.yaml   Größenklassen (ID, Namensschlüssel, Mindestabfluss, Farbe)

Aufruf aus dem Projektordner:  python tools/fluesse.py [--preview <datei.png>]
Benötigt numpy, Pillow, scipy, PyYAML.
"""

import argparse
import heapq
import json
import logging
import struct
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage
import yaml

log = logging.getLogger("fluesse")

ROOT = Path(__file__).resolve().parent.parent
SRC_HEIGHT = ROOT / "lore" / "Version 5 HM.png"
SRC_ZONES = ROOT / "data" / "world" / "klimazonen.bin"
CLIMATE_DATA = ROOT / "lore" / "world" / "climate-data"
SRC_PRECIPITATION = CLIMATE_DATA / "precipitation_annual"
SRC_TEMPERATURE = CLIMATE_DATA / "temperature_annual_mean"
SRC_COURSES = ROOT / "lore" / "flussvorgaben.yaml"
SRC_COMPARE = ROOT / "lore" / "world" / "all-layers" / "06-rivers-raw.png"
OUT_DIR = ROOT / "data" / "world"
OUT_RASTER = OUT_DIR / "fluesse.bin"
OUT_TABLE = OUT_DIR / "fluesse.yaml"

MAGIC = b"TBFL"
FORMAT_VERSION = 1
ZONES_MAGIC = b"TBKZ"
OCEAN_ZONES = {20, 21, 22, 23, 24}
INLAND_WATER_ZONE = 25

HEIGHT_SCALE_M = 8849.0     # welt_konfiguration.md Abschnitt 4, Export
PLANET_RADIUS_M = 6371000.0  # Abschnitt 1, Körperparameter
SECONDS_PER_YEAR = 365.256 * 86400.0

# Die Heightmap ist auf rund 60 % des Landes nahezu flach (unter 2 m Abweichung zur Umgebung). Ohne Unruhe laufen die
# Flüsse dort und auf gleichmäßigen Hängen als gerade, parallele Linien. Annahme: Rauschen im Gelände (Standardabweichung
# 100 m, Strukturen von 1–8 Zellen) und zufällige Reihenfolge auf gleicher Höhe ergeben natürlich verzweigte Flüsse;
# 40 m ließ an steilen Küsten noch parallele Linien, 200 m überdeckt das Relief. Fester Startwert, damit das Ergebnis gleich bleibt.
TERRAIN_NOISE_M = 100.0
TERRAIN_NOISE_OCTAVES = [(1.0, 0.5), (3.0, 1.0), (8.0, 1.0)]  # (Glättung in Zellen, Gewicht)
RANDOM_SEED = 1

# Hauptfluss erst ab diesem Einzugsgebiet. Ohne diese Grenze werden an sehr nassen Küsten schon kurze Küstenbäche zum
# Fluss (eine Zelle liefert dort über 20 m³/s). Annahme: 20.000 km², etwa 60 Zellen.
MIN_CATCHMENT_KM2 = 20000.0

# Größenklassen nach mittlerem Abfluss in m³/s.
# Annahme: Grenzen gesetzt, bis die Dichte der Hauptflüsse zum Vergleichsbild passt.
CLASSES = [
    (1, "RIVER", 150.0, (90, 160, 220)),
    (2, "RIVER_LARGE", 1000.0, (50, 120, 210)),
    (3, "RIVER_GREAT", 5000.0, (20, 70, 180)),
]

# Trockenfluss: Flussbett, das nur in der Regenzeit Wasser führt. Ein Lauf, der ohne Verdunstung ein Hauptfluss wäre
# (Abfluss und Einzugsgebiet wie oben), mit Verdunstung aber unter dem kleinsten Mindestabfluss bleibt (Annahme).
DRY_CLASS = (4, "RIVER_DRY", (200, 140, 80))

# Vorgegebene Verläufe werden als Korridor so tief eingeschnitten, dass sie jede Flutung über das umliegende Gelände
# gewinnen. Im Korridor bleibt das Rauschen erhalten, damit der Fluss darin mäandert statt als Linie zu laufen.
COURSE_DEPTH_M = 1000.0
COURSE_RADIUS_CELLS = 2
COURSE_SLOPE_M = 2.0  # Gefälle je Zelle Richtung Mündung

NEIGHBOURS = [(-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1)]


def read_zones():
    if not SRC_ZONES.is_file():
        raise FileNotFoundError(f"Quelle fehlt: {SRC_ZONES} (zuerst tools/klimazonen.py ausführen)")
    data = SRC_ZONES.read_bytes()
    if data[:4] != ZONES_MAGIC:
        raise ValueError(f"{SRC_ZONES.name}: Kennung {data[:4]!r} statt {ZONES_MAGIC!r}")
    version, width, height = struct.unpack("<III", data[4:16])
    if version != 1 or len(data) != 16 + width * height:
        raise ValueError(f"{SRC_ZONES.name}: Version {version} oder Größe passt nicht")
    return np.frombuffer(data, np.uint8, offset=16).reshape(height, width)


def read_climate(stem, width, height):
    """Liest eine Ebene der Klimadaten (16-Bit-PNG + JSON mit linearer Kodierung) und skaliert sie aufs Zielraster."""
    meta_path, image_path = stem.with_suffix(".json"), stem.with_suffix(".png")
    for path in (meta_path, image_path):
        if not path.is_file():
            raise FileNotFoundError(f"Quelle fehlt: {path}")
    encoding = json.loads(meta_path.read_text(encoding="utf-8"))["encoding"]
    if encoding["type"] != "linear" or encoding["bits"] != 16:
        raise ValueError(f"{meta_path.name}: Kodierung {encoding['type']}/{encoding['bits']} wird nicht unterstützt")
    raw = np.array(Image.open(image_path)).astype(np.float32)
    values = encoding["encoding_min"] + raw / 65535.0 * (encoding["encoding_max"] - encoding["encoding_min"])
    return np.array(Image.fromarray(values, mode="F").resize((width, height), Image.BILINEAR))


def read_height(width, height):
    if not SRC_HEIGHT.is_file():
        raise FileNotFoundError(f"Quelle fehlt: {SRC_HEIGHT}")
    raw = np.array(Image.open(SRC_HEIGHT)).astype(np.float32) / 65535.0 * HEIGHT_SCALE_M
    return np.array(Image.fromarray(raw, mode="F").resize((width, height), Image.BOX))


def runoff_mm(precipitation, temperature):
    """Abfluss = Niederschlag − Verdunstung. Verdunstung nach der Formel von Turc:
    ET = P / sqrt(0,9 + P² / L²), L = 300 + 25 T + 0,05 T³ (P in mm/Jahr, T in °C).
    Annahme: Für Kälte unter etwa −8 °C wird L klein; dort verdunstet fast nichts, darum L mindestens 50."""
    p = np.maximum(precipitation, 0.0)
    t = temperature
    l = np.maximum(300.0 + 25.0 * t + 0.05 * t ** 3, 50.0)
    evaporation = p / np.sqrt(0.9 + (p / l) ** 2)
    return np.maximum(p - evaporation, 0.0)


def cell_area_m2(width, height):
    lat = np.radians(90.0 - (np.arange(height) + 0.5) * 180.0 / height)
    area = PLANET_RADIUS_M ** 2 * (2 * np.pi / width) * (np.pi / height) * np.cos(lat)
    return np.broadcast_to(area[:, None], (height, width))


def terrain_noise(shape, rng):
    noise = np.zeros(shape)
    for sigma, weight in TERRAIN_NOISE_OCTAVES:
        layer = ndimage.gaussian_filter(rng.standard_normal(shape), sigma, mode=("nearest", "wrap"))
        noise += weight * layer / layer.std()
    return TERRAIN_NOISE_M * noise / noise.std()


def flow_network(elevation, ocean, rng):
    """Füllt Senken von der Küste aus (Prioritätsflutung) und legt dabei für jede Landzelle fest, wohin sie abfließt.
    Ergebnis: Ziel je Zelle (−1 = mündet ins Meer) und Reihenfolge von der Mündung flussaufwärts.
    Jede Zelle bekommt genau ein Ziel, das vor ihr erreicht wurde; Kreise sind so ausgeschlossen."""
    height, width = elevation.shape
    receiver = np.full(height * width, -1, np.int64)
    visited = ocean.ravel().copy()
    filled = elevation.ravel().astype(np.float64).copy()
    tie_break = rng.random(height * width)
    heap = []
    # Start: Landzellen an der Küste, sie münden direkt ins Meer
    coast = ndimage.binary_dilation(ocean, np.ones((3, 3), bool)) & ~ocean
    for index in np.flatnonzero(coast.ravel()):
        heapq.heappush(heap, (filled[index], tie_break[index], int(index)))
        visited[index] = True
    order = []
    while heap:
        level, _, index = heapq.heappop(heap)
        order.append(index)
        y, x = divmod(index, width)
        for dy, dx in NEIGHBOURS:
            ny = y + dy
            if ny < 0 or ny >= height:
                continue
            neighbour = ny * width + (x + dx) % width
            if visited[neighbour]:
                continue
            visited[neighbour] = True
            filled[neighbour] = max(filled[neighbour], level)
            receiver[neighbour] = index
            heapq.heappush(heap, (filled[neighbour], tie_break[neighbour], neighbour))
    raised = (filled - elevation.ravel()) > 0.0
    log.info("Fließnetz: %d Landzellen, %d in aufgefüllten Senken", len(order), int(raised.sum()))
    return receiver, np.array(order, np.int64)


def accumulate(local, receiver, order):
    """Abfluss sammeln: von oben nach unten, jede Zelle gibt ihre Menge an ihr Ziel weiter."""
    total = local.ravel().astype(np.float64).copy()
    for index in order[::-1]:
        target = receiver[index]
        if target >= 0:
            total[target] += total[index]
    return total


def course_cells(points, width, height):
    """Zellen entlang eines Verlaufs ([Länge, Breite] je Punkt), von der Quelle zur Mündung, ohne Wiederholung."""
    cells = []
    for (lon0, lat0), (lon1, lat1) in zip(points, points[1:]):
        x0, y0 = (lon0 + 180.0) / 360.0 * width, (90.0 - lat0) / 180.0 * height
        x1, y1 = (lon1 + 180.0) / 360.0 * width, (90.0 - lat1) / 180.0 * height
        steps = int(np.ceil(max(abs(x1 - x0), abs(y1 - y0)) * 2)) + 1
        for t in np.linspace(0.0, 1.0, steps):
            cell = (int(np.clip(y0 + (y1 - y0) * t, 0, height - 1)), int(x0 + (x1 - x0) * t) % width)
            if not cells or cells[-1] != cell:
                cells.append(cell)
    return cells


def carve_courses(elevation, noise, water):
    """Schneidet die vorgegebenen Verläufe als Korridor ein: tief unter das Gelände, zur Mündung hin fallend, mit dem
    Rauschen des Geländes. Der Verlauf muss im Meer oder in einem Binnengewässer enden."""
    if not SRC_COURSES.is_file():
        raise FileNotFoundError(f"Quelle fehlt: {SRC_COURSES}")
    data = yaml.safe_load(SRC_COURSES.read_text(encoding="utf-8")) or {}
    height, width = elevation.shape
    carved = elevation + noise
    for course in data.get("courses") or []:
        course_id = course.get("id")
        points = course.get("points") or []
        if not course_id or len(points) < 2:
            raise ValueError(f"Flussvorgabe {course_id!r}: id und mindestens zwei Punkte nötig")
        for lon, lat in points:
            if not (-180.0 <= lon <= 180.0 and -90.0 <= lat <= 90.0):
                raise ValueError(f"Flussvorgabe {course_id}: Punkt ({lon}, {lat}) liegt nicht auf der Karte")
        cells = course_cells(points, width, height)
        if not water[cells[-1]]:
            raise ValueError(f"Flussvorgabe {course_id}: letzter Punkt liegt nicht im Meer oder in einem Binnengewässer")
        corridor = np.full((height, width), np.inf)
        for step, (y, x) in enumerate(cells):
            for dy in range(-COURSE_RADIUS_CELLS, COURSE_RADIUS_CELLS + 1):
                for dx in range(-COURSE_RADIUS_CELLS, COURSE_RADIUS_CELLS + 1):
                    ny, nx = y + dy, (x + dx) % width
                    if 0 <= ny < height and dy * dy + dx * dx <= COURSE_RADIUS_CELLS ** 2:
                        corridor[ny, nx] = min(corridor[ny, nx], -COURSE_DEPTH_M - (len(cells) - step) * COURSE_SLOPE_M)
        inside = np.isfinite(corridor) & ~water
        carved[inside] = np.minimum(carved[inside], corridor[inside] + noise[inside])
        log.info("Flussvorgabe %s eingeschnitten: %d Zellen", course_id, len(cells))
    return carved


def build():
    zones = read_zones()
    height, width = zones.shape
    ocean = np.isin(zones, list(OCEAN_ZONES))
    lakes = zones == INLAND_WATER_ZONE
    elevation = read_height(width, height)
    # Seen liegen im Höhenmodell auf Meereshöhe; die Flutung füllt sie bis zum Überlauf, so fließen sie weiter ab
    precipitation = read_climate(SRC_PRECIPITATION, width, height)
    temperature = read_climate(SRC_TEMPERATURE, width, height)
    runoff = np.where(ocean, 0.0, runoff_mm(precipitation, temperature))
    log.info("Abfluss: Mittel über Land %.0f mm/Jahr (Niederschlag %.0f mm/Jahr)",
             runoff[~ocean].mean(), precipitation[~ocean].mean())
    local = runoff / 1000.0 * cell_area_m2(width, height) / SECONDS_PER_YEAR  # m³/s je Zelle
    rng = np.random.default_rng(RANDOM_SEED)
    terrain = carve_courses(elevation, terrain_noise(elevation.shape, rng), ocean | lakes)
    receiver, order = flow_network(terrain, ocean, rng)
    discharge = accumulate(local, receiver, order).reshape(height, width)
    rain_only = np.where(ocean, 0.0, precipitation) / 1000.0 * cell_area_m2(width, height) / SECONDS_PER_YEAR
    discharge_without_evaporation = accumulate(rain_only, receiver, order).reshape(height, width)
    catchment = accumulate(np.where(ocean, 0.0, cell_area_m2(width, height) / 1e6), receiver, order).reshape(height, width)
    smallest = CLASSES[0][2]
    rivers = np.zeros((height, width), np.uint8)
    rivers[(discharge < smallest) & (discharge_without_evaporation >= smallest)] = DRY_CLASS[0]
    for class_id, _, minimum, _ in CLASSES:
        rivers[discharge >= minimum] = class_id
    rivers[(catchment < MIN_CATCHMENT_KM2) | ocean | lakes] = 0
    log.info("Größter Abfluss: %.0f m³/s", discharge[~ocean].max())
    return rivers


def write_raster(rivers):
    """Format: 4 Byte Kennung „TBFL“, dann uint32 Version, Breite, Höhe (Little Endian),
    danach Breite × Höhe Bytes Größenklasse (0 = kein Fluss), zeilenweise von Norden nach Süden, Spalten ab 180° W."""
    height, width = rivers.shape
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    with open(OUT_RASTER, "wb") as file:
        file.write(MAGIC)
        file.write(struct.pack("<III", FORMAT_VERSION, width, height))
        file.write(np.ascontiguousarray(rivers, np.uint8).tobytes())
    log.info("Raster geschrieben: %s (%d Bytes)", OUT_RASTER, OUT_RASTER.stat().st_size)


def write_table():
    lines = [
        "# Hauptflüsse, Größenklassen nach mittlerem Abfluss (lore/welt_konfiguration.md Abschnitt 7).",
        "# Erzeugt von tools/fluesse.py, nicht von Hand ändern. 0 im Raster = kein Fluss.",
        f"raster: {OUT_RASTER.name}",
        "classes:",
    ]
    for class_id, key, minimum, color in CLASSES:
        lines.append(f"  - {{ id: {class_id}, name_key: {key}, seasonal: false, min_discharge_m3s: {minimum:g}, "
                     f"color: [{color[0]}, {color[1]}, {color[2]}] }}")
    dry_id, dry_key, dry_color = DRY_CLASS
    lines.append(f"  - {{ id: {dry_id}, name_key: {dry_key}, seasonal: true, min_discharge_m3s: 0, "
                 f"color: [{dry_color[0]}, {dry_color[1]}, {dry_color[2]}] }}")
    OUT_TABLE.write_text("\n".join(lines) + "\n", encoding="utf-8")
    log.info("Klassentabelle geschrieben: %s", OUT_TABLE)


def write_preview(rivers, path):
    """Links das Ergebnis, rechts das Vergleichsbild des gültigen Laufs."""
    zones = read_zones()
    height, width = rivers.shape
    land = ~np.isin(zones, list(OCEAN_ZONES | {INLAND_WATER_ZONE}))
    image = np.where(land[..., None], np.array([200, 195, 175], np.uint8), np.array([30, 40, 60], np.uint8))
    for class_id, _, _, color in CLASSES + [(DRY_CLASS[0], None, None, DRY_CLASS[2])]:
        image[rivers == class_id] = color
    result = Image.fromarray(image.astype(np.uint8))
    compare = Image.open(SRC_COMPARE).convert("RGB").resize((width, height), Image.BOX)
    sheet = Image.new("RGB", (width * 2, height))
    sheet.paste(result, (0, 0))
    sheet.paste(compare, (width, 0))
    sheet.save(path)
    log.info("Vorschau geschrieben: %s", path)


def main():
    sys.stderr.reconfigure(encoding="utf-8")
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(funcName)s: %(message)s")
    parser = argparse.ArgumentParser(description="Erzeugt data/world/fluesse.bin und .yaml")
    parser.add_argument("--preview", type=Path, help="zusätzlich ein Vorschaubild (Ergebnis neben Vergleichsbild) schreiben")
    args = parser.parse_args()
    try:
        rivers = build()
        counts = np.bincount(rivers.ravel(), minlength=DRY_CLASS[0] + 1)
        ids = [class_id for class_id, *_ in CLASSES] + [DRY_CLASS[0]]
        log.info("Zellen je Klasse: %s", {class_id: int(counts[class_id]) for class_id in ids})
        write_raster(rivers)
        write_table()
        if args.preview:
            write_preview(rivers, args.preview)
    except Exception:
        log.exception("Flussebene konnte nicht erzeugt werden")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
