"""Berechnet die Eignung der Pflanzen je Zelle als Ebenen (T-079).

Regeln: lore/welt_konfiguration.md Abschnitt 12, Inhalte: lore/pflanzen.md.
  eignung = K(köppen) × B(boden) × W(wasser) × H(höhe) × Hang × Frost × Inselbonus × rauschen(startwert)
Quellen:
  data/nature/pflanzen.yaml        Pflanzen (Prüfung und Bodengewichte in tools/pflanzen_daten.py)
  data/nature/boeden.yaml          Böden mit Fruchtbarkeit und pH
  lore/regionen.yaml               Regionen (Heimat und Linien), Grenzen in lore/regionen_grenzen.png
  data/world/klimazonen.bin        Land, Meer, Binnengewässer
  data/world/koeppen.bin           Köppen-Klassen
  data/world/gesteinsraeume.bin    Eisschild
  data/world/boeden.bin            Bodenanteile (tools/boeden.py)
  data/world/bodenfeuchte.bin      Wasserkategorien (tools/boeden.py)
  lore/Version 5 HM.png            Höhe und Hangneigung
  lore/world/climate-data/temperature_coldest_month   kältester Monat (frostfrei)
Ergebnis:
  data/world/pflanzen.bin          Ebenen-Datei: je Pflanze ein Layer (Linien je Region <id>_<region>),
                                   0 = kein Vorkommen, 255 = beste Stelle dieser Pflanze
  lore/pflanzen_vielfalt.png       Anzahl Pflanzen je Zelle
  lore/regionen.png                Regionen mit ihren Grenzen
  lore/ebenen/pflanzen/<id>.png    ein Graustufenbild je Layer

Aufruf aus dem Projektordner:  python tools/pflanzen.py
Benötigt numpy, Pillow, scipy, PyYAML.
"""

import logging
import sys
import time
from pathlib import Path
from types import SimpleNamespace

import numpy as np
from PIL import Image
from scipy import ndimage

import boeden
import ebenen
import fluesse
import gesteinsraeume
import pflanzen_daten
from gestein import read_names
from klimazonen import label_wrapped, to_pixel

log = logging.getLogger("pflanzen")

ROOT = Path(__file__).resolve().parent.parent
SRC_KOEPPEN_TABLE = ROOT / "data" / "world" / "koeppen.yaml"
SRC_ROOM_TABLE = ROOT / "data" / "world" / "gesteinsraeume.yaml"
OUT_NAME = "pflanzen.bin"
OUT_MAGIC = b"TBPF"
OUT_DIVERSITY_IMAGE = ROOT / "lore" / "pflanzen_vielfalt.png"
OUT_REGION_IMAGE = ROOT / "lore" / "regionen.png"
CATEGORY = "pflanzen"
MISSING_WATER = {"GEZEITEN"}   # TODO: T-085 – Layer GEZEITEN fehlt in bodenfeuchte.bin, Pflanzen damit bleiben dort leer

# Annahmen (welt_konfiguration.md Abschnitt 12, O - Vorschlag):
RIM_KM = 150.0                 # Randzone: Zellen anderer Klassen bis so weit von der Kernzone
RIM_WEIGHT = 0.5
# Feuchtestufen der Reihe nach; die Nachbarstufe einer passenden zählt abgeschwächt (TROCKEN-Pflanzen auch auf FRISCH).
MOISTURE_ORDER = ["TROCKEN", "FRISCH", "FEUCHT", "NASS"]
ADJACENT_WATER_WEIGHT = 0.5
HANG_FULL_SLOPE = 0.008        # HANG: volles Gewicht ab diesem Gefälle (90 % des Landes liegen unter 0,9 %)
HANG_MIN_FACTOR = 0.25         # ... in der Ebene dieser Anteil
FROST_LIMIT_C = 0.0            # frostfrei: kältester Monat darüber
ISLAND_BONUS = 1.5             # bevorzugt Inseln: Landmassen unter gesteinsraeume.MAINLAND_MIN_KM2
NOISE_SIGMA = 8.0              # Strukturgröße des Rauschens in Zellen
NOISE_AMPLITUDE = 0.3
NOISE_RANGE = (0.4, 1.6)
MIN_SUITABILITY = 0.05         # darunter kein Vorkommen
BASIC_NEEDS = [("Nahrung", {"N"}), ("Holz oder Brennmaterial", {"H", "B"}), ("Faser", {"F"})]
DIVERSITY_BINS = [(0, (90, 90, 90)), (1, (120, 70, 50)), (6, (190, 150, 60)), (11, (210, 210, 90)),
                  (21, (130, 200, 90)), (31, (50, 160, 80)), (46, (20, 100, 60))]
REGION_COLORS = [(220, 120, 80), (120, 170, 70), (230, 200, 90), (170, 110, 200), (230, 240, 250), (90, 200, 160),
                   (200, 90, 140), (150, 150, 90)]


def assign_regions(shape, regions):
    """Region je Zelle (1 = erste in regionen.yaml): Flächen zwischen den Grenzlinien, Linienzellen und Splitter ohne
    Ankerpunkt an die nächstgelegene Region. Der Kartenrand begrenzt, Ost und West laufen nicht um."""
    lines = pflanzen_daten.read_region_lines(shape)
    labels, _ = ndimage.label(~ndimage.binary_dilation(lines))
    owner = np.zeros(labels.max() + 1, np.int32)
    for number, region in enumerate(regions, start=1):
        x, y = to_pixel(*region["point"])
        label = labels[y, x]
        if label == 0:
            raise ValueError(f"Region {region['id']}: Punkt {region['point']} liegt auf einer Grenzlinie")
        if owner[label]:
            other = regions[owner[label] - 1]["id"]
            raise ValueError(f"Regionen {other} und {region['id']} hängen zusammen (Grenzlinie nicht geschlossen)")
        owner[label] = number
    anchored = owner[labels]
    _, (rows, cols) = ndimage.distance_transform_edt(anchored == 0, return_indices=True)
    return anchored[rows, cols].astype(np.uint8), lines


def load_grids(regions):
    zones = ebenen.read_raster("klimazonen.bin", b"TBKZ")
    height, width = zones.shape
    lake = zones == ebenen.INLAND_WATER_ZONE
    ocean = np.isin(zones, list(ebenen.WATER_ZONES)) & ~lake
    land = ~ocean & ~lake
    koeppen_ids = read_names(SRC_KOEPPEN_TABLE, "code")
    koeppen = ebenen.read_raster("koeppen.bin", b"TBKP")
    room_ids = read_names(SRC_ROOM_TABLE, "key")
    ice = land & ((ebenen.read_raster("gesteinsraeume.bin", b"TBGR") == room_ids["EISSCHILD"])
                  | (koeppen == koeppen_ids["EF"]))
    area = ebenen.cell_area_km2(height, width)
    region, lines = assign_regions((height, width), regions)
    labels, _ = label_wrapped(land)
    sizes = np.bincount(labels.ravel(), weights=area.ravel())
    island = land & (sizes[labels] < gesteinsraeume.MAINLAND_MIN_KM2)
    valid = land | lake
    index = np.flatnonzero(valid)
    flat = lambda values: np.ascontiguousarray(values.reshape(-1)[index])
    soils = pflanzen_daten.read_soils()
    soil_layers = ebenen.read_layer_file("boeden.bin", b"TBBO")
    moisture = ebenen.read_layer_file("bodenfeuchte.bin", b"TBBF")
    water = {}
    for category in pflanzen_daten.WATER_CATEGORIES:
        if category in moisture:
            water[category] = flat(moisture[category]).astype(np.float32) / 255.0
        elif category in MISSING_WATER:
            water[category] = np.zeros(index.size, np.float32)
        else:
            raise ValueError(f"bodenfeuchte.bin: Layer {category!r} fehlt (zuerst tools/boeden.py ausführen)")
    elevation, slope = boeden.read_terrain(width, height)
    coldest = fluesse.read_climate(fluesse.CLIMATE_DATA / "temperature_coldest_month", width, height)
    return SimpleNamespace(
        shape=(height, width), land=land, lake=lake, valid=valid, ice=ice, area=area, index=index,
        koeppen_full=koeppen, koeppen_ids=koeppen_ids, koeppen=flat(koeppen), is_lake=flat(lake),
        soils=soils, soil_matrix=np.stack([flat(soil_layers[s["id"]]) for s in soils], axis=1).astype(np.float32) / 255.0,
        water=water, elevation=flat(elevation), slope=flat(slope), coldest=flat(coldest),
        ocean_km=flat(ebenen.wrapped_distance_km(ocean)), island=flat(island),
        region_full=region, region=flat(region), region_lines=lines, rim_cache={},
    )


def rim_mask(grids, core_numbers):
    """Zellen bis RIM_KM von den Kernklassen entfernt (gecacht je Klassenmenge)."""
    if core_numbers not in grids.rim_cache:
        core = np.isin(grids.koeppen_full, core_numbers) & grids.land
        near = ebenen.wrapped_distance_km(core) <= RIM_KM if core.any() else np.zeros(grids.shape, bool)
        grids.rim_cache[core_numbers] = near.reshape(-1)[grids.index]
    return grids.rim_cache[core_numbers]


def in_range(values, bounds):
    low, high = bounds
    result = np.ones(values.shape, bool)
    if low is not None:
        result &= values >= low
    if high is not None:
        result &= values <= high
    return result


def water_factor(grids, categories):
    """Summe der passenden Kategorien, Nachbarstufen der Feuchte mit ADJACENT_WATER_WEIGHT, höchstens 1."""
    weights = {c: 1.0 for c in categories}
    for category in categories:
        if category in MOISTURE_ORDER:
            position = MOISTURE_ORDER.index(category)
            for neighbor in MOISTURE_ORDER[max(position - 1, 0):position + 2]:
                weights.setdefault(neighbor, ADJACENT_WATER_WEIGHT)
    return np.minimum(1.0, sum(weight * grids.water[c] for c, weight in weights.items()))


def suitability(plant, grids):
    """Eignung je gültiger Zelle (Land und Binnengewässer) ohne Rauschen."""
    climate = np.zeros(grids.index.size, np.float32)
    water = water_factor(grids, plant.water)
    listed, core = [], []
    for code, (weight, condition) in plant.climate.items():
        number = grids.koeppen_ids[code]
        listed.append(number)
        in_class = grids.koeppen == number
        if condition is None:
            climate[in_class] = np.maximum(climate[in_class], weight)
            if weight >= 1.0:
                core.append(number)
            continue
        allowed = in_class.copy()
        if condition["elevation_m"] is not None:
            allowed &= in_range(grids.elevation, condition["elevation_m"])
        if condition["coast_km"] is not None:
            allowed &= grids.ocean_km <= condition["coast_km"]
        climate[allowed] = np.maximum(climate[allowed], weight)
        if condition["water"]:
            water[in_class] = water_factor(grids, condition["water"])[in_class]
    rim = rim_mask(grids, tuple(sorted(core))) & ~np.isin(grids.koeppen, listed)
    climate[rim] = np.maximum(climate[rim], RIM_WEIGHT)
    soil = grids.soil_matrix @ plant.soil_weights
    soil[grids.is_lake] = 1.0
    if plant.soil_free:
        soil[:] = 1.0
    result = climate * soil * water
    if plant.elevation_m is not None:
        result *= in_range(grids.elevation, plant.elevation_m)
    if plant.hang:
        result *= np.clip(grids.slope / HANG_FULL_SLOPE, HANG_MIN_FACTOR, 1.0)
    if plant.frost_free:
        result *= grids.coldest > FROST_LIMIT_C
    if plant.island_bonus:
        result *= np.where(grids.island, ISLAND_BONUS, 1.0)
    return result.astype(np.float32)


def noise_factor(layer_id, grids):
    noise = ebenen.smooth_noise(grids.shape, NOISE_SIGMA, ebenen.seeded_rng(layer_id)).reshape(-1)[grids.index]
    return np.clip(1.0 + NOISE_AMPLITUDE * noise, *NOISE_RANGE).astype(np.float32)


def normalize(raw):
    present = raw >= MIN_SUITABILITY
    values = np.zeros(raw.shape, np.uint8)
    if present.any():
        values[present] = np.clip(np.rint(raw[present] / raw.max() * 255.0), 1, 255)
    return values


def layer_keys(plant, regions):
    """(Layer-ID, erlaubte Regionsnummern oder None = weltweit); Linien je Region der Heimat."""
    if not plant.line:
        return [(plant.id, plant.homeland)]
    numbers = plant.homeland or list(range(1, len(regions) + 1))
    return [(f"{plant.id}_{regions[number - 1]['id']}", [number]) for number in numbers]


def build_layers(plants, grids, regions):
    """Layer (ID, Werte je gültiger Zelle) und Zähler für Vielfalt und Grundbedürfnisse."""
    land_cells = ~grids.is_lake & ~grids.ice.reshape(-1)[grids.index]
    area = grids.area.reshape(-1)[grids.index]
    land_area = area[land_cells].sum()
    diversity = np.zeros(grids.index.size, np.uint16)
    needs = {name: np.zeros(grids.index.size, bool) for name, _ in BASIC_NEEDS}
    layers = []
    for plant in plants:
        if not plant.active:
            log.info("%s: fiktiv und nicht aktiv, übersprungen", plant.id)
            continue
        for flag in ("shade", "fire_pioneer", "fog"):
            if plant.entry.get(flag):
                log.info("%s: %s noch nicht berücksichtigt (TODO, welt_konfiguration.md Abschnitt 12)", plant.id, flag)
        missing = MISSING_WATER.intersection(plant.water)
        if missing:
            log.warning("%s: Wasserkategorie %s fehlt in den Weltdaten (T-085)", plant.id, ", ".join(sorted(missing)))
        base = suitability(plant, grids)
        for layer_id, allowed in layer_keys(plant, regions):
            raw = base * noise_factor(layer_id, grids)
            if allowed is not None:
                raw = np.where(np.isin(grids.region, allowed), raw, 0.0)
            values = normalize(raw)
            present = values > 0
            if not present.any():
                if not plant.line:
                    log.warning("%s: kein Vorkommen", layer_id)
                else:
                    log.info("%s: kein Vorkommen in dieser Region, kein Layer", layer_id)
                    continue
            cover = float(area[present & land_cells].sum() / land_area * 100.0)
            if "STEHEND" in plant.water:
                log.info("%s: auf %d Zellen der Binnengewässer", layer_id, int((present & grids.is_lake).sum()))
            log.info("%s: Abdeckung %.2f %% des Landes, Maximum %.3f, Mittel %.3f, Schwellwert %.2f", layer_id,
                     cover, float(raw.max()), float(raw[present].mean()) if present.any() else 0.0, MIN_SUITABILITY)
            diversity += present
            for name, codes in BASIC_NEEDS:
                if plant.use & codes:
                    needs[name] |= present
            layers.append((layer_id, values))
    return layers, diversity, needs, land_cells, area


def check_coverage(diversity, needs, land_cells, area, grids, regions):
    """Prüfungen nach welt_konfiguration.md Abschnitt 12: fehlendes wird geloggt, nicht aufgefüllt."""
    land_area = area[land_cells].sum()
    empty = land_cells & (diversity == 0)
    if empty.any():
        log.warning("Landzellen ohne Pflanze (außer Eis): %d, %.2f %% des Landes", int(empty.sum()),
                    float(area[empty].sum() / land_area * 100.0))
    for name, covered in needs.items():
        lacking = land_cells & ~covered
        share = float(area[lacking].sum() / land_area * 100.0)
        (log.warning if share > 0 else log.info)("Grundbedürfnis %s fehlt auf %.2f %% des Landes", name, share)
    # Je Region: welcher Anteil ihres Landes ohne Grundbedürfnis bleibt (Handel oder Ersatz, z. B. Tierhäute statt Faser)
    for number, region in enumerate(regions, start=1):
        in_region = land_cells & (grids.region == number)
        region_area = area[in_region].sum()
        parts = [f"{name} {area[in_region & ~covered].sum() / region_area * 100.0:.1f} %" for name, covered in needs.items()]
        log.info("Region %s: Pflanzen je Zelle im Mittel %.1f; ohne %s", region["id"],
                 float(diversity[in_region].mean()), ", ".join(parts))
    log.info("Pflanzen je Landzelle: Mittel %.1f, Median %d, Maximum %d", float(diversity[land_cells].mean()),
             int(np.median(diversity[land_cells])), int(diversity[land_cells].max()))


def to_full(values, grids):
    full = np.zeros(grids.shape, values.dtype)
    full.reshape(-1)[grids.index] = values
    return full


def write_images(layers, diversity, grids, regions):
    counts = to_full(diversity, grids)
    image = np.zeros(grids.shape + (3,), np.uint8)
    image[:] = ebenen.SEA_COLOR
    entries = []
    for i, (low, color) in enumerate(DIVERSITY_BINS):
        high = DIVERSITY_BINS[i + 1][0] - 1 if i + 1 < len(DIVERSITY_BINS) else None
        mask = grids.valid & (counts >= low) & (counts <= (high if high is not None else counts.max()))
        image[mask] = color
        label = f"{low}" if high == low else (f"{low}–{high}" if high is not None else f"ab {low}")
        entries.append((color, f"{label} Pflanzen"))
    ebenen.with_legend(image, "Pflanzenvielfalt", entries).save(OUT_DIVERSITY_IMAGE)
    log.info("Bild geschrieben: %s", OUT_DIVERSITY_IMAGE)
    if len(regions) > len(REGION_COLORS):
        raise ValueError(f"Zu viele Regionen für die Farbtabelle ({len(REGION_COLORS)})")
    palette = np.array([(0, 0, 0)] + REGION_COLORS[:len(regions)], np.uint8)
    colors = palette[grids.region_full].astype(np.float32)
    sea = colors * 0.3 + np.array(ebenen.SEA_COLOR, np.float32) * 0.7
    image = np.where(grids.land[..., None], colors, sea).astype(np.uint8)
    image[grids.region_lines] = (255, 0, 255)
    entries = [(REGION_COLORS[i], region["id"]) for i, region in enumerate(regions)]
    ebenen.with_legend(image, "Regionen der Pflanzen", entries).save(OUT_REGION_IMAGE)
    log.info("Bild geschrieben: %s", OUT_REGION_IMAGE)
    ebenen.ensure_layer_image_dir()
    for layer_id, values in layers:
        ebenen.write_layer_image(CATEGORY, layer_id, values, grids.valid)
    log.info("Einzelbilder geschrieben: %s (%d)", ebenen.LAYER_IMAGE_DIR / CATEGORY, len(layers))


def main():
    sys.stderr.reconfigure(encoding="utf-8")
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(funcName)s: %(message)s")
    started = time.perf_counter()
    log.info("Start Pflanzen")
    try:
        regions = pflanzen_daten.read_regions()
        grids = load_grids(regions)
        plants = pflanzen_daten.read_plants(grids.koeppen_ids, grids.soils, regions)
        layers, diversity, needs, land_cells, area = build_layers(plants, grids, regions)
        check_coverage(diversity, needs, land_cells, area, grids, regions)
        full_layers = [(layer_id, to_full(values, grids)) for layer_id, values in layers]
        ebenen.write_layer_file(OUT_NAME, OUT_MAGIC, full_layers)
        write_images(full_layers, diversity, grids, regions)
    except Exception:
        log.exception("Pflanzen konnten nicht erzeugt werden")
        return 1
    log.info("Ende Pflanzen nach %.1f s", time.perf_counter() - started)
    return 0


if __name__ == "__main__":
    sys.exit(main())
