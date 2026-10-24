"""Berechnet die Lagerstätten als Ebenen (T-082).

Regeln: lore/welt_konfiguration.md Abschnitt 10, Inhalte: lore/materialien_und_boeden.md Abschnitte 5.2–5.6.
Quellen:
  data/nature/materialien.yaml     Lagerstätten (rule.kind deposit) mit ihren Bedingungen
  data/nature/boeden.yaml          Boden-IDs (Reihenfolge der Layer in boeden.bin)
  lore/gesteinsraeume.yaml         Ketten, auf die Bedingungen verweisen können (chains)
  data/world/klimazonen.bin, koeppen.bin, gesteinsraeume.bin, gestein.bin, boeden.bin, bodenfeuchte.bin,
  fluesse.bin, fliessrichtung.bin
  lore/Version 5 HM.png            Heightmap (Höhe, Gefälle)
Ergebnis:
  data/world/lagerstaetten.bin     Ebenen-Datei: je Lagerstätte ein Layer 0–255 (255 = ergiebigste Stelle)
  lore/lagerstaetten.png           wichtige Lagerstätten farbig über dem Land
  lore/ebenen/lagerstaetten/<id>.png   ein Graustufenbild je Layer

Aufruf aus dem Projektordner:  python tools/lagerstaetten.py
Benötigt numpy, Pillow, scipy, PyYAML.
"""

import logging
import sys
import time
from pathlib import Path
from types import SimpleNamespace

import numpy as np
from scipy import ndimage
import yaml

import bedingungen
import bodenfeuchte
import boeden
import ebenen
import gesteinsraeume
import seifen
import vorkommen
from gestein import read_names, room_weights
from klimazonen import label_wrapped

log = logging.getLogger("lagerstaetten")

ROOT = Path(__file__).resolve().parent.parent
SRC_MATERIALS = ROOT / "data" / "nature" / "materialien.yaml"
SRC_SOILS = ROOT / "data" / "nature" / "boeden.yaml"
SRC_KOEPPEN_TABLE = ROOT / "data" / "world" / "koeppen.yaml"
SRC_ROOM_TABLE = ROOT / "data" / "world" / "gesteinsraeume.yaml"
OUT_NAME = "lagerstaetten.bin"
OUT_MAGIC = b"TBLA"
OUT_MAIN_IMAGE = ROOT / "lore" / "lagerstaetten.png"
CATEGORY = "lagerstaetten"

DISTRIBUTIONS = {"FLAECHIG", "KLUMPIG", "ABGELEITET", "PUNKTUELL", "SEIFE"}
META_KEYS = {"kind", "sites", "sites_per_landmass", "placer"}
# Annahmen (welt_konfiguration.md Abschnitt 10, O - Vorschlag):
SITE_NOISE_SIGMA = 3.0        # sites: Auswahl der Fundorte nach Eignung × Rauschen dieser Strukturgröße
SITE_MIN_KM = 1500.0          # Mindestabstand der Fundorte untereinander
SITE_EDGE = 0.1               # Fundort reicht so weit, wie das geglättete Zentrum über diesem Anteil liegt
PLACER_POINT_SHARE = 0.5      # PUNKTUELL mit Seife: Anteil der Zielabdeckung für die primären Fundorte

# Übersichtsbild: Gruppen in Zeichenreihenfolge (häufige zuerst), seltene Gruppen um eine Zelle verbreitert
OVERVIEW_GROUPS = [
    ("Eisen", ["eisen_bandeisen", "eisen_raseneisen", "eisen_laterit", "eisen_magnetit", "eisen_haematit", "eisensand",
               "meteoreisen"], (170, 70, 50), False),
    ("Kohle", ["steinkohle", "braunkohle"], (25, 25, 25), False),
    ("Salz", ["steinsalz", "meersalz"], (245, 245, 245), False),
    ("Kupfer", ["kupfer_gediegen", "kupfer_porphyr", "kupfer_sediment", "kupfer_gang"], (40, 190, 160), False),
    ("Silber", ["silber", "silber_gediegen"], (160, 170, 230), True),
    ("Gold", ["gold_primaer", "gold_seife"], (255, 205, 30), True),
    ("Zinn", ["zinn", "zinn_seife"], (240, 60, 200), True),
]
LAND_COLOR = (95, 95, 95)


def read_deposits(known):
    if not SRC_MATERIALS.is_file():
        raise FileNotFoundError(f"Quelle fehlt: {SRC_MATERIALS}")
    data = yaml.safe_load(SRC_MATERIALS.read_text(encoding="utf-8")) or {}
    targets = data.get("rarity_classes") or {}
    deposits = [m for m in data.get("materials") or [] if m.get("rule", {}).get("kind") == "deposit"]
    known = dict(known, layers=set(known["layers"]) | {m["id"] for m in deposits})
    for material in deposits:
        where = f"{SRC_MATERIALS.name}: {material['id']}"
        if material.get("rarity") not in targets:
            raise ValueError(f"{where}: unbekannte Seltenheit {material.get('rarity')!r}")
        if material.get("distribution") not in DISTRIBUTIONS:
            raise ValueError(f"{where}: Verteilung {material.get('distribution')!r} wird nicht erzeugt")
        rule = material["rule"]
        bedingungen.validate({k: v for k, v in rule.items() if k not in META_KEYS}, where, known)
        if material["distribution"] == "SEIFE" and not bedingungen.source_ids(rule):
            raise ValueError(f"{where}: SEIFE braucht eine Quelle")
        if rule.get("sites") and material["distribution"] != "PUNKTUELL":
            raise ValueError(f"{where}: sites gilt nur für PUNKTUELL")
        if rule.get("sites_per_landmass") and not rule.get("sites"):
            raise ValueError(f"{where}: sites_per_landmass braucht sites")
    log.info("Lagerstätten geladen: %d", len(deposits))
    return deposits, targets


def load_grids():
    zones = ebenen.read_raster("klimazonen.bin", b"TBKZ")
    height, width = zones.shape
    lake = zones == ebenen.INLAND_WATER_ZONE
    ocean = np.isin(zones, list(ebenen.WATER_ZONES)) & ~lake
    land = ~ocean & ~lake
    koeppen_ids = read_names(SRC_KOEPPEN_TABLE, "code")
    codes = np.empty(256, object)
    codes[:] = ""
    for code, number in koeppen_ids.items():
        codes[number] = code
    room_ids = read_names(SRC_ROOM_TABLE, "key")
    rooms = ebenen.read_raster("gesteinsraeume.bin", b"TBGR")
    soil_ids = [s["id"] for s in (yaml.safe_load(SRC_SOILS.read_text(encoding="utf-8")) or {}).get("soils") or []]
    soils = ebenen.read_layer_file("boeden.bin", b"TBBO")
    moisture = ebenen.read_layer_file("bodenfeuchte.bin", b"TBBF")
    rivers = ebenen.read_raster("fluesse.bin", b"TBFL")
    elevation, slope = boeden.read_terrain(width, height)
    grids = SimpleNamespace(
        land=land, lake=lake, ocean=ocean, area=ebenen.cell_area_km2(height, width),
        ice=rooms == room_ids["EISSCHILD"], koeppen_codes=codes[ebenen.read_raster("koeppen.bin", b"TBKP")],
        room_keys=boeden.room_keys(rooms, room_ids), room_weights=room_weights(rooms, land, room_ids),
        soils=soils, wetland=moisture["feuchtgebiet"].astype(np.float64) / 255.0, rivers=rivers,
        river_band=bodenfeuchte.river_band(rivers), elevation=elevation, slope=slope,
        ocean_km=ebenen.wrapped_distance_km(ocean), lake_km=ebenen.wrapped_distance_km(lake),
    )
    # Festland wie in tools/gesteinsraeume.py: Landmassen ab MAINLAND_MIN_KM2, nummeriert ab 1 (0 = kein Festland)
    labels, count = label_wrapped(land)
    sizes = np.bincount(labels.ravel(), weights=grids.area.ravel(), minlength=count + 1)
    big = np.flatnonzero(sizes >= gesteinsraeume.MAINLAND_MIN_KM2)
    big = big[big > 0]
    numbering = np.zeros(count + 1, np.int32)
    numbering[big] = np.arange(1, big.size + 1)
    grids.mainland, grids.mainland_count = numbering[labels], int(big.size)
    network = seifen.FlowNetwork(ebenen.read_raster("fliessrichtung.bin", b"TBFR"), rivers, lake, slope, land)
    known = {"rooms": set(room_ids), "koeppen": set(koeppen_ids), "soils": set(soil_ids)}
    return grids, network, known


def place_sites(material, suitability, grids, rng):
    """sites: genau N Fundorte an den besten Stellen (Eignung × Rauschen), mindestens SITE_MIN_KM voneinander.
    sites_per_landmass: zuerst so viele Fundorte auf jedem Festland, dann der Rest an den besten Stellen weltweit."""
    rule = material["rule"]
    count = rule["sites"]
    value = np.where(grids.land & ~grids.ice,
                     suitability * np.exp(ebenen.smooth_noise(suitability.shape, SITE_NOISE_SIGMA, rng)), 0.0)
    height, width = value.shape
    lat = np.radians(90.0 - (np.arange(height) + 0.5) * 180.0 / height)
    lon = np.radians(-180.0 + (np.arange(width) + 0.5) * 360.0 / width)
    order = np.argsort(value.ravel())[::-1]
    order = order[value.flat[order] > 0]
    picked = []

    def pick(candidates, limit):
        added = 0
        for index in candidates:
            if added == limit or len(picked) == count:
                return
            y, x = divmod(int(index), width)
            if all(great_circle_km(lat[y], lon[x], lat[py], lon[px]) >= SITE_MIN_KM for py, px in picked):
                picked.append((y, x))
                added += 1

    per_landmass = rule.get("sites_per_landmass", 0)
    for label in range(1, grids.mainland_count + 1):
        on_land = order[grids.mainland.ravel()[order] == label]
        before = len(picked)
        pick(on_land, per_landmass)
        if per_landmass and len(picked) == before:
            log.warning("%s: kein geeigneter Fundort auf Festland %d", material["id"], label)
    pick(order, count)
    if len(picked) < count:
        log.warning("%s: nur %d von %d Fundorten gefunden", material["id"], len(picked), count)
    centers = np.zeros(value.shape, np.float32)
    for y, x in picked:
        centers[y, x] = 1.0
    field = ndimage.gaussian_filter(centers, vorkommen.POINT_SIGMA, mode=("nearest", "wrap"))
    field /= max(field.max(), 1e-9)
    layer = np.where(field >= SITE_EDGE, np.rint(1.0 + field * 254.0), 0.0).astype(np.uint8)
    log.info("%s: %d Fundorte bei %s", material["id"], len(picked),
             ", ".join(f"({np.degrees(lon[x]):.0f}°, {np.degrees(lat[y]):.0f}°)" for y, x in picked))
    return layer


def great_circle_km(lat0, lon0, lat1, lon1):
    a = np.sin((lat1 - lat0) / 2) ** 2 + np.cos(lat0) * np.cos(lat1) * np.sin((lon1 - lon0) / 2) ** 2
    return 2 * ebenen.PLANET_RADIUS_KM * np.arcsin(np.sqrt(min(max(a, 0.0), 1.0)))


def generate(material, target, conditions, layers, grids, network):
    rule = material["rule"]
    suitability = conditions.factor({k: v for k, v in rule.items() if k not in META_KEYS}, layers)
    allowed = grids.land & ~grids.ice
    rng = ebenen.seeded_rng(material["id"])
    if rule.get("sites"):
        return place_sites(material, suitability, grids, rng)
    if material["distribution"] == "SEIFE":
        value = np.where(allowed, network.carry(np.where(allowed, suitability, 0.0)), 0.0)
    else:
        value = np.where(allowed, suitability * vorkommen.distribution_noise(material["distribution"],
                                                                           suitability.shape, rng), 0.0)
        if rule.get("placer"):
            points, _, _ = vorkommen.calibrate(material["id"], value, target * PLACER_POINT_SHARE, grids.land, grids.area)
            value = np.where(allowed, network.carry(points.astype(np.float64) / 255.0), 0.0)
    values, possible, threshold = vorkommen.calibrate(material["id"], value, target, grids.land, grids.area)
    vorkommen.log_result(material, values, target, possible, threshold, grids.land, grids.area)
    return values


def build():
    grids, network, known = load_grids()
    rocks = ebenen.read_layer_file("gestein.bin", b"TBGS")
    chains = {chain["id"]: chain for chain in gesteinsraeume.read_rules()[0]}
    deposits, targets = read_deposits(dict(known, layers=set(rocks), chains=set(chains)))
    conditions = bedingungen.Conditions(grids, chains)
    layers = dict(rocks)
    for material in vorkommen.topological(deposits, lambda m: bedingungen.source_ids(m["rule"])):
        layers[material["id"]] = generate(material, targets[material["rarity"]], conditions, layers, grids, network)
    return [(m["id"], layers[m["id"]]) for m in deposits], grids


def write_images(layers, land):
    by_id = dict(layers)
    for _, ids, _, _ in OVERVIEW_GROUPS:
        for layer_id in ids:
            if layer_id not in by_id:
                raise ValueError(f"Übersichtsbild: Lagerstätte {layer_id!r} fehlt")
    image = np.where(land[..., None], np.array(LAND_COLOR, np.uint8), np.array(ebenen.SEA_COLOR, np.uint8))
    area = ebenen.cell_area_km2(*land.shape)
    entries = []
    for name, ids, color, widen in OVERVIEW_GROUPS:
        mask = np.logical_or.reduce([by_id[i] > 0 for i in ids])
        entries.append((color, f"{name}  {ebenen.coverage_percent(mask, land, area):.2f} %"))
        if widen:
            mask = ndimage.binary_dilation(mask, np.ones((3, 3), bool)) & land
        image[mask] = color
    ebenen.with_legend(image, "Lagerstätten", entries).save(OUT_MAIN_IMAGE)
    log.info("Bild geschrieben: %s", OUT_MAIN_IMAGE)
    ebenen.ensure_layer_image_dir()
    for layer_id, values in layers:
        ebenen.write_layer_image(CATEGORY, layer_id, values, land)
    log.info("Einzelbilder geschrieben: %s (%d)", ebenen.LAYER_IMAGE_DIR / CATEGORY, len(layers))


def main():
    sys.stderr.reconfigure(encoding="utf-8")
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(funcName)s: %(message)s")
    started = time.perf_counter()
    log.info("Start Lagerstätten")
    try:
        layers, grids = build()
        ebenen.write_layer_file(OUT_NAME, OUT_MAGIC, layers)
        write_images(layers, grids.land)
    except Exception:
        log.exception("Lagerstätten konnten nicht erzeugt werden")
        return 1
    log.info("Ende Lagerstätten nach %.1f s", time.perf_counter() - started)
    return 0


if __name__ == "__main__":
    sys.exit(main())
