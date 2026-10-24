"""Ordnet jeder Landzelle einen Gesteinsraum zu (T-076).

Regeln: lore/welt_konfiguration.md Abschnitt 9, Zuordnung der Ketten und Inseln: lore/materialien_und_boeden.md Abschnitt 2.1.
Quellen:
  data/world/klimazonen.bin      Land, Meer und Binnengewässer (tools/klimazonen.py)
  lore/Version 5 HM.png          Heightmap, Grauwert × 8849 m
  lore/gesteinsraeume.yaml       Ketten und Inselgruppen mit ihrem Typ
Ergebnis:
  data/world/gesteinsraeume.bin  Kopf + ein Byte Typ je Zelle (0 = Meer oder Binnengewässer), Raster der Klimazonen
  data/world/gesteinsraeume.yaml Typentabelle (ID, Schlüssel, Farbe)
  lore/gesteinsraeume.png        Bild des Ergebnisses

Aufruf aus dem Projektordner:  python tools/gesteinsraeume.py
Benötigt numpy, Pillow, scipy, PyYAML.
"""

import heapq
import logging
import struct
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage
import yaml

import ebenen
from ebenen import KM_PER_DEG, PLANET_RADIUS_KM, cell_area_km2, seeded_rng
from klimazonen import ice_sheet_mask, label_wrapped, lat_of_rows, lon_of_cols

log = logging.getLogger("gesteinsraeume")

ROOT = Path(__file__).resolve().parent.parent
SRC_HEIGHT = ROOT / "lore" / "Version 5 HM.png"
SRC_RULES = ROOT / "lore" / "gesteinsraeume.yaml"
OUT_DIR = ROOT / "data" / "world"
OUT_RASTER = OUT_DIR / "gesteinsraeume.bin"
OUT_TABLE = OUT_DIR / "gesteinsraeume.yaml"
OUT_IMAGE = ROOT / "lore" / "gesteinsraeume.png"

MAGIC = b"TBGR"
FORMAT_VERSION = 1

HEIGHT_SCALE_M = 8849.0

# Typen: ID, Schlüssel, Farbe (materialien_und_boeden.md Abschnitt 2)
TYPES = [
    (1, "KRATON", (205, 190, 150)),
    (2, "BECKEN", (240, 225, 120)),
    (3, "ALTGEBIRGE", (60, 150, 75)),
    (4, "KOLLISION", (150, 60, 190)),
    (5, "OPHIOLITH", (20, 90, 70)),
    (6, "KUESTENBOGEN", (220, 50, 45)),
    (7, "INSELBOGEN", (240, 140, 30)),
    (8, "GRABEN", (0, 180, 185)),
    (9, "HOTSPOT", (35, 80, 220)),
    (10, "EISSCHILD", (245, 248, 252)),
]
TYPE_ID = {key: type_id for type_id, key, _ in TYPES}

# Annahmen (welt_konfiguration.md Abschnitt 9, O - Werte nach dem ersten Bild):
CHAIN_REACH_FACTOR = 2.0       # Kettenzellen bis k · Breite von der Polylinie
RELIEF_SIGMA_DEG = 3.0         # Umland = geglättete Höhe in diesem Umkreis
RELIEF_MIN_M = 200.0           # erhöht = so viel über dem Umland ...
MOUNTAIN_MIN_M = 1500.0        # ... oder absolut so hoch
HIGHLAND_MIN_M = 1000.0        # Erhebungen ohne Kette ab dieser Höhe → ALTGEBIRGE
HIGHLAND_MIN_CELLS = 30        # kleinere Flecken bleiben, wie sie sind
MAINLAND_MIN_KM2 = 2.5e6       # Festland: Westkontinent, Ostkontinent, Nordwestland, Südland
REMOTE_ISLAND_KM = 500.0       # Insel ohne Gruppe weiter vom Festland → HOTSPOT ...
HOTSPOT_ISLAND_MAX_KM2 = 1e5   # ... sofern nicht größer (größere sind abgetrennte Kontinentstücke)
GROUP_SNAP_DEG = 2.0           # Inseln einer Gruppe: Land in diesem Umkreis um einen Blob-Mittelpunkt
OPHIOLITE_BAND_CELLS = 2       # Flanke von KOLLISION
OPHIOLITE_NOISE_SIGMA = 2.0
OPHIOLITE_SHARE = 0.35         # Anteil der Flanke, der OPHIOLITH wird
BASIN_LOWLAND_M = 400.0        # Tiefland
BASIN_FORELAND_KM = 400.0      # Streifen auf der Landseite von KOLLISION und KUESTENBOGEN
BASIN_LAKE_KM = 150.0          # Tiefland um Binnengewässer
BASIN_SINK_DEPTH_M = 50.0      # Senke: so tief unter ihrem Überlauf
# Küstenebenen an ruhigen Küsten: junge Ablagerungen statt Kraton (materialien_und_boeden.md Abschnitt 2, 2026-10-24).
# Die Breite schwankt großräumig zwischen 0 und dem Doppelten, damit kein gleichmäßiger Ring um die Küsten entsteht.
COASTAL_PLAIN_M = 150.0        # unter dieser Höhe ...
COASTAL_PLAIN_KM = 150.0       # ... bis im Mittel so weit von der Küste ...
QUIET_COAST_KM = 500.0         # ... und so weit von aktiven Ketten (KOLLISION, KUESTENBOGEN, INSELBOGEN, OPHIOLITH)
COASTAL_PLAIN_NOISE_SIGMA = 15.0  # Strukturgröße der Breitenschwankung, in Zellen




def read_height(width, height):
    if not SRC_HEIGHT.is_file():
        raise FileNotFoundError(f"Quelle fehlt: {SRC_HEIGHT}")
    raw = np.array(Image.open(SRC_HEIGHT)).astype(np.float32) / 65535.0 * HEIGHT_SCALE_M
    return np.array(Image.fromarray(raw, mode="F").resize((width, height), Image.BOX))


def read_rules():
    if not SRC_RULES.is_file():
        raise FileNotFoundError(f"Quelle fehlt: {SRC_RULES}")
    data = yaml.safe_load(SRC_RULES.read_text(encoding="utf-8")) or {}
    chains, groups = data.get("chains") or [], data.get("island_groups") or []
    for entry in chains + groups:
        if entry.get("type") not in TYPE_ID:
            raise ValueError(f"{SRC_RULES.name}: {entry.get('id')!r} hat unbekannten Typ {entry.get('type')!r}")
        for lon, lat in entry.get("points") or []:
            if not (-180.0 <= lon <= 180.0 and -90.0 <= lat <= 90.0):
                raise ValueError(f"{SRC_RULES.name}: {entry['id']}: Punkt ({lon}, {lat}) liegt nicht auf der Karte")
    for chain in chains:
        if len(chain.get("points") or []) < 2 or not chain.get("width_deg"):
            raise ValueError(f"{SRC_RULES.name}: Kette {chain.get('id')!r} braucht width_deg und mindestens zwei Punkte")
    log.info("Regeln geladen: %d Ketten, %d Inselgruppen", len(chains), len(groups))
    return chains, groups




def wrap_deg(delta):
    return (delta + 180.0) % 360.0 - 180.0


def distance_to_polyline_km(points, lon, lat):
    """Abstand jeder Zelle zur Polylinie in km (örtlich flach gerechnet, Ost-West über die Datumsgrenze)."""
    scale_x = np.cos(np.radians(lat)) * KM_PER_DEG
    best = np.full(np.broadcast(lon, lat).shape, np.inf)
    for (lon0, lat0), (lon1, lat1) in zip(points, points[1:]):
        px, py = wrap_deg(lon - lon0) * scale_x, (lat - lat0) * KM_PER_DEG
        bx, by = wrap_deg(lon1 - lon0) * scale_x, (lat1 - lat0) * KM_PER_DEG
        t = np.clip((px * bx + py * by) / np.maximum(bx * bx + by * by, 1e-9), 0.0, 1.0)
        best = np.minimum(best, np.hypot(px - t * bx, py - t * by))
    return best




def wrapped_distance_km(mask_from):
    """Abstand zur nächsten True-Zelle in km (Großkreis zur nach Zellen nächsten Zelle), horizontal umlaufend."""
    height, width = mask_from.shape
    pad = width // 4
    padded = np.concatenate([mask_from[:, -pad:], mask_from, mask_from[:, :pad]], axis=1)
    _, (iy, ix) = ndimage.distance_transform_edt(~padded, return_indices=True)
    iy, ix = iy[:, pad:pad + width], (ix[:, pad:pad + width] - pad) % width
    lat0 = np.radians(lat_of_rows(height))[:, None]
    lon0 = np.radians(lon_of_cols(width))[None, :]
    lat1, lon1 = np.radians(lat_of_rows(height))[iy], np.radians(lon_of_cols(width))[ix]
    a = np.sin((lat1 - lat0) / 2) ** 2 + np.cos(lat0) * np.cos(lat1) * np.sin((lon1 - lon0) / 2) ** 2
    return 2 * PLANET_RADIUS_KM * np.arcsin(np.sqrt(np.clip(a, 0.0, 1.0)))


def elevated_mask(elevation, land):
    """Erhöhtes Gelände: deutlich über dem geglätteten Umland oder absolut hoch."""
    sigma = RELIEF_SIGMA_DEG * elevation.shape[1] / 360.0
    weight = ndimage.gaussian_filter(land.astype(np.float32), sigma, mode=("nearest", "wrap"))
    base = ndimage.gaussian_filter(np.where(land, elevation, 0.0), sigma, mode=("nearest", "wrap")) / np.maximum(weight, 1e-6)
    return land & ((elevation - base >= RELIEF_MIN_M) | (elevation >= MOUNTAIN_MIN_M))


def assign_chains(result, distance, chains, land, elevated, lon, lat):
    """Regel 1: erhöhtes Gelände nahe einer Kette bekommt deren Typ; bei Überlappung die nähere Kette."""
    for chain in chains:
        reach = CHAIN_REACH_FACTOR * chain["width_deg"] * KM_PER_DEG
        near = distance_to_polyline_km(chain["points"], lon, lat)
        cells = land & (near <= reach) & (elevated if chain.get("elevated", True) else True) & (near < distance)
        result[cells] = TYPE_ID[chain["type"]]
        distance[cells] = near[cells]
        log.info("Kette %s (%s): %d Zellen", chain["id"], chain["type"], int(cells.sum()))
        if not cells.any():
            log.warning("Kette %s: keine Zelle zugeordnet", chain["id"])


def islands_near(labels, mainland, point):
    """Alle Inseln mit Land im Umkreis GROUP_SNAP_DEG um einen Punkt (ein Blob kann in mehrere Inseln zerfallen sein).
    Festland zählt nicht."""
    height, width = labels.shape
    x, y = int((point[0] + 180.0) / 360.0 * width) % width, int(np.clip((90.0 - point[1]) / 180.0 * height, 0, height - 1))
    radius_y = int(np.ceil(GROUP_SNAP_DEG * height / 180.0))
    radius_x = int(np.ceil(radius_y / max(np.cos(np.radians(point[1])), 0.1)))
    rows = np.arange(max(y - radius_y, 0), min(y + radius_y + 1, height))
    cols = np.arange(x - radius_x, x + radius_x + 1) % width
    window = labels[rows][:, cols]
    dy = (rows - y)[:, None] / radius_y
    dx = ((cols - x + width // 2) % width - width // 2)[None, :] / radius_x
    found = np.unique(window[(window > 0) & (dy ** 2 + dx ** 2 <= 1.0)])
    return [int(label) for label in found if not mainland[label]]


def assign_islands(result, chains, groups, labels, mainland):
    """Regel 2: Inseln einer Gruppe bekommen deren Typ. Inseln mit einer Kette vom Typ INSELBOGEN oder HOTSPOT werden
    ganz zu diesem Typ; Kettenzellen behalten ihren Typ."""
    assigned = np.zeros(mainland.shape, bool)
    whole_island_types = {"INSELBOGEN", "HOTSPOT"}
    group_ids = {group["id"] for group in groups}
    sources = [(group["id"], group["type"], group["points"]) for group in groups]
    sources += [(chain["id"], chain["type"], chain["points"]) for chain in chains if chain["type"] in whole_island_types]
    for source_id, type_key, points in sources:
        found = 0
        for point in points:
            near = islands_near(labels, mainland, point)
            if not near and source_id in group_ids:
                log.warning("Inselgruppe %s: Punkt %s trifft keine Insel (Festland oder Meer)", source_id, point)
            for label in near:
                if assigned[label]:
                    continue
                cells = (labels == label) & (result == 0)
                result[cells] = TYPE_ID[type_key]
                assigned[label] = True
                found += 1
        log.info("Inseln %s (%s): %d Inseln", source_id, type_key, found)
    return assigned


def assign_highlands(result, land, elevation):
    """Regel 3: Erhebungen ohne Kette ab HIGHLAND_MIN_M → ALTGEBIRGE (nur zusammenhängende Flächen ab HIGHLAND_MIN_CELLS).
    Flächen, die an eine Kette grenzen, sind deren Flanke und bekommen den Typ der nächsten Kettenzelle."""
    candidates = land & (result == 0) & (elevation >= HIGHLAND_MIN_M)
    labels, count = label_wrapped(candidates)
    if count == 0:
        return
    sizes = np.bincount(labels.ravel())
    keep = sizes >= HIGHLAND_MIN_CELLS
    keep[0] = False
    chain = result > 0
    touching = np.zeros(count + 1, bool)
    touching[np.unique(labels[ndimage.binary_dilation(chain, np.ones((3, 3), bool)) & (labels > 0)])] = True
    flank = keep[labels] & touching[labels]
    if flank.any():
        _, (iy, ix) = ndimage.distance_transform_edt(~chain, return_indices=True)
        result[flank] = result[iy, ix][flank]
    old = keep[labels] & ~touching[labels]
    result[old] = TYPE_ID["ALTGEBIRGE"]
    log.info("Regel 3 (Erhebungen ohne Kette): %d Flächen, %d Zellen ALTGEBIRGE, %d Zellen als Flanke einer Kette",
             int((keep & ~touching).sum()), int(old.sum()), int(flank.sum()))


def assign_remote_islands(result, labels, count, mainland, assigned, has_chain, land, sizes):
    """Regel 4: Inseln ohne Gruppe und ohne Kette werden HOTSPOT, wenn sie weit vom Festland liegen und klein sind.
    Große Inseln weit draußen sind abgetrennte Kontinentstücke und bleiben beim Standard."""
    distance = wrapped_distance_km(mainland[labels] & land)
    remote = 0
    for label in range(1, count + 1):
        if mainland[label] or assigned[label] or has_chain[label] or sizes[label] > HOTSPOT_ISLAND_MAX_KM2:
            continue
        cells = labels == label
        if distance[cells].min() > REMOTE_ISLAND_KM:
            result[cells & (result == 0)] = TYPE_ID["HOTSPOT"]
            remote += 1
    log.info("Regel 4 (entfernte Inseln): %d Inseln → HOTSPOT", remote)


def assign_ophiolite(result):
    """Regel 5: schmale, rauschgestörte Streifen an den Flanken von KOLLISION."""
    collision = result == TYPE_ID["KOLLISION"]
    inner = ndimage.binary_erosion(collision, np.ones((3, 3), bool), iterations=OPHIOLITE_BAND_CELLS)
    band = collision & ~inner
    rng = seeded_rng("ophiolith")
    noise = ndimage.gaussian_filter(rng.standard_normal(result.shape), OPHIOLITE_NOISE_SIGMA, mode=("nearest", "wrap"))
    threshold = np.quantile(noise[band], 1.0 - OPHIOLITE_SHARE) if band.any() else np.inf
    cells = band & (noise >= threshold)
    result[cells] = TYPE_ID["OPHIOLITH"]
    log.info("Regel 5 (Ophiolith): %d Zellen", int(cells.sum()))


def filled_depth(elevation, sea):
    """Tiefe unter dem Überlauf je Zelle: Senken von der Küste aus füllen (Prioritätsflutung, ohne Geländerauschen)."""
    height, width = elevation.shape
    filled = elevation.astype(np.float64).ravel().copy()
    visited = sea.ravel().copy()
    heap = []
    coast = ndimage.binary_dilation(sea, np.ones((3, 3), bool)) & ~sea
    for index in np.flatnonzero(coast.ravel()):
        heapq.heappush(heap, (filled[index], int(index)))
        visited[index] = True
    while heap:
        level, index = heapq.heappop(heap)
        y, x = divmod(index, width)
        for dy in (-1, 0, 1):
            ny = y + dy
            if ny < 0 or ny >= height:
                continue
            for dx in (-1, 0, 1):
                neighbour = ny * width + (x + dx) % width
                if visited[neighbour]:
                    continue
                visited[neighbour] = True
                filled[neighbour] = max(filled[neighbour], level)
                heapq.heappush(heap, (filled[neighbour], neighbour))
    return (filled - elevation.ravel()).reshape(height, width)


def assign_basins(result, land, elevation, lakes, ocean):
    """Regel 6: Tiefland vor KOLLISION und KUESTENBOGEN, um Binnengewässer, in Senken und Küstenebenen an ruhigen
    Küsten → BECKEN. Vorland liegt auf der Landseite: näher am Gebirge als am Meer."""
    lowland = land & (result == 0) & (elevation < BASIN_LOWLAND_M)
    mountains = np.isin(result, [TYPE_ID["KOLLISION"], TYPE_ID["KUESTENBOGEN"], TYPE_ID["OPHIOLITH"]])
    to_mountains = wrapped_distance_km(mountains)
    to_ocean = wrapped_distance_km(ocean)
    foreland = lowland & (to_mountains <= BASIN_FORELAND_KM) & (to_mountains < to_ocean)
    lakeside = lowland & (wrapped_distance_km(lakes) <= BASIN_LAKE_KM) if lakes.any() else np.zeros_like(lowland)
    sinks = land & (result == 0) & (filled_depth(elevation, ocean) >= BASIN_SINK_DEPTH_M)
    active = np.isin(result, [TYPE_ID["KOLLISION"], TYPE_ID["KUESTENBOGEN"], TYPE_ID["INSELBOGEN"], TYPE_ID["OPHIOLITH"]])
    noise = ebenen.smooth_noise(land.shape, COASTAL_PLAIN_NOISE_SIGMA, seeded_rng("kuestenebene"))
    width_km = COASTAL_PLAIN_KM * np.clip(1.0 + noise, 0.0, 2.0)
    coastal = land & (result == 0) & (elevation < COASTAL_PLAIN_M) & (to_ocean <= width_km) \
        & (wrapped_distance_km(active) > QUIET_COAST_KM)
    cells = foreland | lakeside | sinks | coastal
    result[cells] = TYPE_ID["BECKEN"]
    log.info("Regel 6 (Becken): %d Zellen (Vorland %d, Binnengewässer %d, Senken %d, Küstenebenen %d)", int(cells.sum()),
             int(foreland.sum()), int(lakeside.sum()), int(sinks.sum()), int(coastal.sum()))


def build():
    zones = ebenen.read_raster("klimazonen.bin", b"TBKZ")
    height, width = zones.shape
    land = ~np.isin(zones, list(ebenen.WATER_ZONES))
    lakes = zones == ebenen.INLAND_WATER_ZONE
    ocean = ~land & ~lakes
    elevation = read_height(width, height)
    chains, groups = read_rules()
    lon = np.broadcast_to(lon_of_cols(width)[None, :], (height, width))
    lat = np.broadcast_to(lat_of_rows(height)[:, None], (height, width))

    labels, count = label_wrapped(land)
    sizes = np.bincount(labels.ravel(), weights=cell_area_km2(height, width).ravel(), minlength=count + 1)
    mainland = sizes >= MAINLAND_MIN_KM2
    mainland[0] = False
    log.info("Landmassen: %d, davon Festland %d (%s km²)", count, int(mainland.sum()),
             ", ".join(f"{s:,.0f}" for s in sorted(sizes[mainland], reverse=True)))

    result = np.zeros((height, width), np.uint8)
    distance = np.full((height, width), np.inf)
    assign_chains(result, distance, chains, land, elevated_mask(elevation, land), lon, lat)
    has_chain = np.zeros(count + 1, bool)
    has_chain[np.unique(labels[result > 0])] = True
    assigned = assign_islands(result, chains, groups, labels, mainland)
    assign_highlands(result, land, elevation)
    assign_remote_islands(result, labels, count, mainland, assigned, has_chain, land, sizes)
    assign_ophiolite(result)
    assign_basins(result, land, elevation, lakes, ocean)
    ice = ice_sheet_mask() & land
    result[ice] = TYPE_ID["EISSCHILD"]
    log.info("Regel 7 (Eisschild): %d Zellen", int(ice.sum()))
    rest = land & (result == 0)
    result[rest] = TYPE_ID["KRATON"]
    log.info("Regel 8 (Kraton): %d Zellen", int(rest.sum()))
    return result, land


def write_raster(values):
    """Format: 4 Byte Kennung „TBGR“, dann uint32 Version, Breite, Höhe (Little Endian),
    danach Breite × Höhe Bytes Typ (0 = Meer oder Binnengewässer), zeilenweise von Norden nach Süden, Spalten ab 180° W."""
    height, width = values.shape
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    with open(OUT_RASTER, "wb") as file:
        file.write(MAGIC)
        file.write(struct.pack("<III", FORMAT_VERSION, width, height))
        file.write(np.ascontiguousarray(values, np.uint8).tobytes())
    log.info("Raster geschrieben: %s (%d Bytes)", OUT_RASTER, OUT_RASTER.stat().st_size)


def write_table():
    lines = [
        "# Gesteinsräume (lore/welt_konfiguration.md Abschnitt 9, lore/materialien_und_boeden.md Abschnitt 2).",
        "# Erzeugt von tools/gesteinsraeume.py, nicht von Hand ändern. 0 im Raster = Meer oder Binnengewässer.",
        f"raster: {OUT_RASTER.name}",
        "types:",
    ]
    for type_id, key, color in TYPES:
        lines.append(f"  - {{ id: {type_id}, key: {key}, color: [{color[0]}, {color[1]}, {color[2]}] }}")
    OUT_TABLE.write_text("\n".join(lines) + "\n", encoding="utf-8")
    log.info("Typentabelle geschrieben: %s", OUT_TABLE)


def write_image(values):
    palette = np.zeros((256, 3), np.uint8)
    palette[0] = (30, 40, 60)
    for type_id, _, color in TYPES:
        palette[type_id] = color
    Image.fromarray(palette[values]).save(OUT_IMAGE)
    log.info("Bild geschrieben: %s", OUT_IMAGE)


def main():
    sys.stderr.reconfigure(encoding="utf-8")
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(funcName)s: %(message)s")
    try:
        result, land = build()
        if (result[land] == 0).any() or (result[~land] != 0).any():
            raise ValueError("Ergebnis unvollständig: Landzelle ohne Typ oder Meer mit Typ")
        area = cell_area_km2(*result.shape)
        total = area[land].sum()
        shares = {key: round(float(area[result == type_id].sum() / total * 100), 1) for type_id, key, _ in TYPES}
        log.info("Anteil am Land (flächengewichtet, %%): %s", shares)
        write_raster(result)
        write_table()
        write_image(result)
    except Exception:
        log.exception("Gesteinsräume konnten nicht erzeugt werden")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
