"""Erzeugt die vereinfachten Klimazonen als Datenebene (T-067).

Regeln und Zonen-IDs: lore/welt_konfiguration.md, Abschnitt 5.
Quellen (alle aus demselben Lauf des World Climate Lab bzw. dessen Eingaben):
  lore/world/all-layers/02-climate_zones-raw.png  Köppen-Klassen als Legendenfarben, 8192 x 4096 (Lauf 2026-10-10)
  lore/Version 5 HM.png          Heightmap, 16 Bit, 8192 x 4096 (Eingabe des Laufs)
  lore/Version5 Meermaske.png    255 = Land, 0 = Meer, 8192 x 4096 (Eingabe des Laufs)
Ergebnis:
  data/world/klimazonen.bin      Kopf + ein Byte Zonen-ID je Pixel (Format siehe write_raster)
  data/world/klimazonen.yaml     Zonentabelle (ID, Namensschlüssel, Farbe)

Aufruf aus dem Projektordner:  python tools/klimazonen.py [--preview <datei.png>]
Benötigt numpy, Pillow, scipy.
"""

import argparse
import logging
import struct
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

log = logging.getLogger("klimazonen")

ROOT = Path(__file__).resolve().parent.parent
SRC_CLIMATE = ROOT / "lore" / "world" / "all-layers" / "02-climate_zones-raw.png"
SRC_HEIGHT = ROOT / "lore" / "Version 5 HM.png"
SRC_MASK = ROOT / "lore" / "Version5 Meermaske.png"
OUT_DIR = ROOT / "data" / "world"
OUT_RASTER = OUT_DIR / "klimazonen.bin"
OUT_TABLE = OUT_DIR / "klimazonen.yaml"

WIDTH, HEIGHT = 2160, 1080
MAGIC = b"TBKZ"
FORMAT_VERSION = 1

# Heightmap-Grauwert = Höhe / 8849 m (welt_konfiguration.md Abschnitt 4, Export).
PEAK_HEIGHT_M = 8849.0
HIGH_MOUNTAIN_M = 2500.0

# Legendenfarben des Bildes (aus 02-climate_zones-framed.png ausgelesen, exakt gleich den Rohfarben)
LEGEND = {
    (0, 0, 255): "Af", (0, 119, 255): "Am", (70, 169, 250): "Aw", (121, 186, 236): "As",
    (255, 0, 0): "BWh", (255, 150, 149): "BWk", (245, 163, 1): "BSh", (255, 219, 99): "BSk",
    (255, 255, 0): "Csa", (198, 199, 0): "Csb", (150, 150, 0): "Csc",
    (150, 255, 150): "Cwa", (99, 199, 100): "Cwb", (50, 150, 51): "Cwc",
    (198, 255, 78): "Cfa", (102, 255, 51): "Cfb", (51, 199, 1): "Cfc",
    (255, 0, 255): "Dsa", (198, 0, 199): "Dsb", (150, 50, 149): "Dsc", (150, 100, 149): "Dsd",
    (171, 177, 255): "Dwa", (90, 119, 219): "Dwb", (76, 81, 181): "Dwc", (50, 0, 135): "Dwd",
    (0, 255, 255): "Dfa", (56, 199, 255): "Dfb", (0, 126, 125): "Dfc", (0, 69, 94): "Dfd",
    (178, 178, 178): "ET", (104, 104, 104): "EF",
    (247, 248, 248): "WI", (184, 193, 204): "WS", (122, 138, 162): "WC", (93, 105, 123): "WT",
    (59, 70, 84): "WW", (44, 52, 63): "WH", (28, 34, 42): "WE", (8, 11, 16): "WB",
}

# Zonen: ID, Namensschlüssel, Köppen-Klassen, Farbe (welt_konfiguration.md Abschnitt 5)
ZONES = [
    (1, "ZONE_TROPICAL_RAINFOREST", ["Af", "Am"], (15, 100, 35)),
    (2, "ZONE_SAVANNA", ["Aw", "As"], (160, 195, 60)),
    (3, "ZONE_HOT_DESERT", ["BWh"], (240, 175, 90)),
    (4, "ZONE_COLD_DESERT", ["BWk"], (200, 170, 150)),
    (5, "ZONE_STEPPE", ["BSh", "BSk"], (235, 230, 150)),
    (6, "ZONE_MEDITERRANEAN", ["Csa", "Csb", "Csc"], (215, 115, 70)),
    (7, "ZONE_HUMID_SUBTROPICAL", ["Cfa", "Cwa"], (40, 155, 110)),
    (8, "ZONE_TEMPERATE_FOREST", ["Cfb", "Cfc", "Cwb", "Cwc"], (110, 195, 85)),
    (9, "ZONE_CONTINENTAL", ["Dfa", "Dfb", "Dwa", "Dwb", "Dsa", "Dsb"], (150, 125, 195)),
    (10, "ZONE_TAIGA", ["Dfc", "Dfd", "Dwc", "Dwd", "Dsc", "Dsd"], (30, 90, 80)),
    (11, "ZONE_TUNDRA", ["ET"], (165, 175, 155)),
    (12, "ZONE_ICE", ["EF"], (245, 248, 252)),
    (13, "ZONE_HIGH_MOUNTAINS", [], (120, 95, 85)),
    (20, "ZONE_TROPICAL_SEA", ["WW", "WH", "WE", "WB"], (35, 85, 165)),
    (21, "ZONE_REEF", [], (60, 175, 205)),
    (22, "ZONE_TEMPERATE_SEA", ["WT"], (35, 65, 115)),
    (23, "ZONE_COLD_SEA", ["WC"], (70, 90, 110)),
    (24, "ZONE_PACK_ICE", ["WI", "WS"], (200, 215, 230)),
    (25, "ZONE_INLAND_WATER", [], (85, 140, 210)),
]

LAND_ZONES = list(range(1, 13))
SEA_ORDER = [20, 22, 23, 24]  # Ordinalwerte für die Glättung: warm → kalt
SOUTH_CHAIN_SEED = (4.0, 22.0)
SOUTH_CHAIN_MAX_LAT = 29.0
ICE_SHEET = (-52.0, -15.0, 58.0, 86.0)  # Länge von, bis; Breite von, bis
MODE_FILTER_SIZE = 5
SMALL_AREA_PX = 40
SEA_SIGMA = (3.0, 5.0)  # (Breite, Länge) in px. Annahme: „Gauß 3 × 5 px“ = Sigma je Achse
REEF_MAX_LAT = 28.0
REEF_MAX_DIST_PX = 10


def lat_of_rows(height):
    return 90.0 - (np.arange(height) + 0.5) * 180.0 / height


def lon_of_cols(width):
    return -180.0 + (np.arange(width) + 0.5) * 360.0 / width


def to_pixel(lon, lat):
    return int((lon + 180.0) / 360.0 * WIDTH), int((90.0 - lat) / 180.0 * HEIGHT)


def ice_sheet_mask():
    lon_from, lon_to, lat_from, lat_to = ICE_SHEET
    lon = lon_of_cols(WIDTH)[None, :]
    lat = lat_of_rows(HEIGHT)[:, None]
    return (lon >= lon_from) & (lon <= lon_to) & (lat >= lat_from) & (lat <= lat_to)


def label_wrapped(mask):
    """Zusammenhangskomponenten (4er-Nachbarschaft), die über die Datumsgrenze hinweg verbunden sind."""
    labels, count = ndimage.label(mask)
    parent = np.arange(count + 1)

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i

    left, right = labels[:, 0], labels[:, -1]
    for a, b in zip(left[(left > 0) & (right > 0)], right[(left > 0) & (right > 0)]):
        ra, rb = find(a), find(b)
        if ra != rb:
            parent[max(ra, rb)] = min(ra, rb)
    roots = np.array([find(i) for i in range(count + 1)])
    _, compact = np.unique(roots, return_inverse=True)
    return compact.reshape(-1)[labels], int(compact.max())


def wrapped_distance(mask_from):
    """Abstand in px zum nächsten True-Pixel, horizontal umlaufend."""
    pad = WIDTH // 4
    padded = np.concatenate([mask_from[:, -pad:], mask_from, mask_from[:, :pad]], axis=1)
    return ndimage.distance_transform_edt(~padded)[:, pad:pad + WIDTH]


def fill_from_nearest(values, valid):
    """Setzt ungültige Pixel auf den Wert des nächsten gültigen Pixels."""
    if valid.all():
        return values
    _, (iy, ix) = ndimage.distance_transform_edt(~valid, return_indices=True)
    return values[iy, ix]


def load_sources():
    for path in (SRC_CLIMATE, SRC_HEIGHT, SRC_MASK):
        if not path.is_file():
            raise FileNotFoundError(f"Quelle fehlt: {path}")
    climate = Image.open(SRC_CLIMATE).convert("RGB").resize((WIDTH, HEIGHT), Image.NEAREST)
    height_raw = np.array(Image.open(SRC_HEIGHT)).astype(np.float32)
    if height_raw.max() <= 0:
        raise ValueError("Heightmap ist leer")
    # Mittelwert je Zielpixel statt Stichprobe, damit Gipfel nicht zufällig ausfallen oder hinzukommen
    height = np.array(Image.fromarray(height_raw / 65535.0 * PEAK_HEIGHT_M, mode="F").resize((WIDTH, HEIGHT), Image.BOX))
    land = np.array(Image.open(SRC_MASK).convert("L").resize((WIDTH, HEIGHT), Image.NEAREST)) > 127
    log.info("Quellen geladen: Klima %s, höchster Punkt %.0f m, Land %.1f %%", SRC_CLIMATE.name, height_raw.max() / 65535.0 * PEAK_HEIGHT_M, land.mean() * 100)
    return np.array(climate), height, land


def classify(climate_rgb):
    """Legendenfarbe → Zone. Unbekannte Farben brechen ab, statt geraten zu werden."""
    code_to_zone = {code: zone_id for zone_id, _, codes, _ in ZONES for code in codes}
    packed = (climate_rgb[..., 0].astype(np.int32) << 16) | (climate_rgb[..., 1].astype(np.int32) << 8) | climate_rgb[..., 2]
    zones = np.zeros(packed.shape, np.uint8)
    for value in np.unique(packed):
        rgb = (int(value) >> 16, (int(value) >> 8) & 255, int(value) & 255)
        if rgb not in LEGEND:
            raise ValueError(f"Farbe {rgb} steht nicht in der Legende")
        zones[packed == value] = code_to_zone[LEGEND[rgb]]
    return zones


def align_to_mask(zones, land):
    """Küste der Meermaske gilt: Landpixel mit Meeresfarbe bekommen die nächste Landzone und umgekehrt."""
    is_land_zone = np.isin(zones, LAND_ZONES)
    wrong_land = land & ~is_land_zone
    wrong_sea = ~land & is_land_zone
    log.info("Küstenabgleich: %d Landpixel ohne Landzone, %d Meerpixel mit Landzone", wrong_land.sum(), wrong_sea.sum())
    result = np.where(land, fill_from_nearest(zones, is_land_zone), fill_from_nearest(zones, ~is_land_zone))
    return result.astype(np.uint8)


def mode_filter_land(zones, land):
    """Regel 1: 5 × 5-Modusfilter auf Land. Bei Gleichstand bleibt die eigene Zone."""
    counts = np.stack([
        ndimage.uniform_filter(((zones == z) & land).astype(np.float32), MODE_FILTER_SIZE, mode=("nearest", "wrap"))
        for z in LAND_ZONES])
    own = np.stack([(zones == z) for z in LAND_ZONES]).astype(np.float32)
    best = np.array(LAND_ZONES, np.uint8)[np.argmax(counts + own * 1e-3, axis=0)]
    changed = land & (best != zones)
    log.info("Regel 1 (Modusfilter): %d Pixel geändert", changed.sum())
    return np.where(land, best, zones).astype(np.uint8)


def south_chain(zones, land):
    """Regel 2: Südkette grüner, nur B-Klimate (Zonen 3 und 4) nach Breite umfärben."""
    labels, _ = label_wrapped(land)
    seed_x, seed_y = to_pixel(*SOUTH_CHAIN_SEED)
    seed_label = labels[seed_y, seed_x]
    if seed_label == 0:
        raise ValueError(f"Startpunkt der Südkette {SOUTH_CHAIN_SEED} liegt nicht an Land")
    lat = np.broadcast_to(lat_of_rows(HEIGHT)[:, None], zones.shape)
    area = (labels == seed_label) & (lat < SOUTH_CHAIN_MAX_LAT) & np.isin(zones, [3, 4])
    greener = np.where(lat > 24.5, 6, np.where(lat >= 17.5, 5, 2)).astype(np.uint8)
    log.info("Regel 2 (Südkette): %d Pixel umgefärbt", area.sum())
    return np.where(area, greener, zones).astype(np.uint8)


def high_mountains(zones, land, height, ice_sheet):
    """Regel 3: Höhe ≥ 2500 m → Hochgebirge, außer im Eisschild."""
    area = land & (height >= HIGH_MOUNTAIN_M) & ~ice_sheet
    log.info("Regel 3 (Hochgebirge): %d Pixel", area.sum())
    return np.where(area, 13, zones).astype(np.uint8)


def dissolve_small_areas(zones, land, ice_sheet):
    """Regel 4: Landflächen unter 40 px gehen in der häufigsten Nachbarzone auf (nicht 13, Zone 12 nur außerhalb des Eisschilds)."""
    zones = zones.copy()
    for round_no in range(1, 11):
        dissolved = 0
        for zone in LAND_ZONES:
            labels, count = label_wrapped(land & (zones == zone))
            if count == 0:
                continue
            sizes = np.bincount(labels.ravel())
            boxes = ndimage.find_objects(labels)
            for index in np.nonzero(sizes < SMALL_AREA_PX)[0]:
                if index == 0:
                    continue
                rows, cols = boxes[index - 1]
                # Ausschnitt mit 1 px Rand; Flächen an der Datumsgrenze haben einen breiten Ausschnitt, das ist nur langsamer
                box = (slice(max(rows.start - 1, 0), rows.stop + 1), slice(max(cols.start - 1, 0), cols.stop + 1))
                part = labels[box] == index
                if zone == 12 and (part & ice_sheet[box]).any():
                    continue
                ring = ndimage.binary_dilation(part, structure=ndimage.generate_binary_structure(2, 1)) & ~part & land[box]
                neighbours = zones[box][ring]
                neighbours = neighbours[neighbours != zone]
                if neighbours.size == 0:
                    continue  # Kleine Insel mit nur einer Zone bleibt
                zones[box][part] = np.bincount(neighbours).argmax()
                dissolved += 1
        log.info("Regel 4 (Kleinflächen), Runde %d: %d Flächen aufgelöst", round_no, dissolved)
        if dissolved == 0:
            return zones
    log.warning("Regel 4: nach 10 Runden noch nicht stabil")
    return zones


def inland_water(land):
    """Meerflächen ohne Verbindung zum Ozean (größte Meeresfläche) sind Binnengewässer."""
    labels, count = label_wrapped(~land)
    sizes = np.bincount(labels.ravel())
    sizes[0] = 0
    ocean = sizes.argmax()
    lakes = ~land & (labels != ocean)
    log.info("Binnengewässer: %d Flächen, %d Pixel (Ozean %d Pixel)", count - 1, lakes.sum(), sizes[ocean])
    return lakes


def smooth_sea(zones, land):
    """Regel 5: Meereszonen als Ordinalwerte glätten (nur über Meer gewichtet)."""
    sea = ~land
    ordinal = np.zeros(zones.shape, np.float32)
    for index, zone in enumerate(SEA_ORDER):
        ordinal[zones == zone] = index
    weight = ndimage.gaussian_filter(sea.astype(np.float32), SEA_SIGMA, mode=("nearest", "wrap"))
    value = ndimage.gaussian_filter(np.where(sea, ordinal, 0), SEA_SIGMA, mode=("nearest", "wrap"))
    smoothed = np.rint(value / np.maximum(weight, 1e-6)).clip(0, len(SEA_ORDER) - 1).astype(int)
    result = np.where(sea, np.array(SEA_ORDER, np.uint8)[smoothed], zones)
    log.info("Regel 5 (Meer geglättet): %d Pixel geändert", (result != zones).sum())
    return result.astype(np.uint8)


def reefs(zones, land):
    """Regel 6: Tropisches oder gemäßigtes Meer nahe der Küste in niedrigen Breiten → Riff."""
    lat = np.abs(lat_of_rows(HEIGHT))[:, None]
    near_coast = wrapped_distance(land) <= REEF_MAX_DIST_PX
    area = np.isin(zones, [20, 22]) & (lat < REEF_MAX_LAT) & near_coast
    log.info("Regel 6 (Riff): %d Pixel", area.sum())
    return np.where(area, 21, zones).astype(np.uint8)


def build():
    climate, height, land = load_sources()
    ice_sheet = ice_sheet_mask()
    zones = align_to_mask(classify(climate), land)
    zones = mode_filter_land(zones, land)
    zones = south_chain(zones, land)
    zones = high_mountains(zones, land, height, ice_sheet)
    zones = dissolve_small_areas(zones, land, ice_sheet)
    zones = smooth_sea(zones, land)
    zones = reefs(zones, land)
    zones[inland_water(land)] = 25
    return zones


def write_raster(zones):
    """Format: 4 Byte Kennung „TBKZ“, dann uint32 Version, Breite, Höhe (Little Endian),
    danach Breite × Höhe Bytes Zonen-ID, zeilenweise von Norden nach Süden, Spalten ab 180° W."""
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    with open(OUT_RASTER, "wb") as file:
        file.write(MAGIC)
        file.write(struct.pack("<III", FORMAT_VERSION, WIDTH, HEIGHT))
        file.write(np.ascontiguousarray(zones, np.uint8).tobytes())
    log.info("Raster geschrieben: %s (%d Bytes)", OUT_RASTER, OUT_RASTER.stat().st_size)


def write_table():
    lines = [
        "# Vereinfachte Klimazonen (lore/welt_konfiguration.md Abschnitt 5).",
        "# Erzeugt von tools/klimazonen.py, nicht von Hand ändern. kind: land, sea oder inland_water.",
        f"raster: {OUT_RASTER.name}",
        "zones:",
    ]
    for zone_id, key, _, color in ZONES:
        kind = "land" if zone_id in LAND_ZONES or zone_id == 13 else "inland_water" if zone_id == 25 else "sea"
        lines.append(f"  - {{ id: {zone_id}, name_key: {key}, kind: {kind}, color: [{color[0]}, {color[1]}, {color[2]}] }}")
    OUT_TABLE.write_text("\n".join(lines) + "\n", encoding="utf-8")
    log.info("Zonentabelle geschrieben: %s", OUT_TABLE)


def write_preview(zones, path):
    palette = np.zeros((256, 3), np.uint8)
    for zone_id, _, _, color in ZONES:
        palette[zone_id] = color
    Image.fromarray(palette[zones]).save(path)
    log.info("Vorschau geschrieben: %s", path)


def main():
    sys.stderr.reconfigure(encoding="utf-8")
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(funcName)s: %(message)s")
    parser = argparse.ArgumentParser(description="Erzeugt data/world/klimazonen.bin und .yaml")
    parser.add_argument("--preview", type=Path, help="zusätzlich ein Vorschaubild der Zonen schreiben")
    args = parser.parse_args()
    try:
        zones = build()
        unknown = set(np.unique(zones).tolist()) - {zone_id for zone_id, *_ in ZONES}
        if unknown:
            raise ValueError(f"Unbekannte Zonen im Ergebnis: {sorted(unknown)}")
        counts = np.bincount(zones.ravel(), minlength=256)
        log.info("Pixel je Zone: %s", {zone_id: int(counts[zone_id]) for zone_id, *_ in ZONES})
        write_raster(zones)
        write_table()
        if args.preview:
            write_preview(zones, args.preview)
    except Exception:
        log.exception("Klimazonen konnten nicht erzeugt werden")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
