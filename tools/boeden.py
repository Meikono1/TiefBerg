"""Berechnet Bodenfeuchte und Böden als Ebenen (T-078).

Regeln: lore/welt_konfiguration.md Abschnitt 11, Inhalte: lore/materialien_und_boeden.md Abschnitt 6.
Quellen:
  data/nature/boeden.yaml          Böden, zonale Zuordnung, Übergänge und Überschreibungen
  data/nature/materialien.yaml     welche Layer von gestein.bin Gesteinsanteile sind
  data/world/klimazonen.bin        Land, Meer, Binnengewässer
  data/world/koeppen.bin           Köppen-Klassen
  data/world/gesteinsraeume.bin    Gesteinsräume (Becken, Eisschild)
  data/world/gestein.bin           Gesteinsanteile (tools/gestein.py)
  data/world/fluesse.bin           Hauptflüsse und Trockenflüsse (tools/fluesse.py)
  data/world/gezeiten.bin          Tidenhub des Meeres (tools/gezeiten.py)
  lore/Version 5 HM.png            Heightmap 8192 × 4096 (Hangneigung, Senken)
  lore/world/climate-data/soil_water_index, rainfall_annual   Bodenwasser und Regen (Lauf 2026-10-23)
  lore/world/all-layers/02-climate_zones-raw.png   Köppen-Klassen vor den Regeln (Angleich der Bodenfeuchte)
Ergebnis:
  data/world/bodenfeuchte.bin, lore/bodenfeuchte.png, lore/ebenen/bodenfeuchte/   (tools/bodenfeuchte.py)
  data/world/boeden.bin            Ebenen-Datei: je Boden ein Anteil (Summe je Landzelle 255) und hauptboden (Position in
                                   boeden.yaml ab 1, 0 = Wasser)
  lore/hauptboden.png              vorherrschender Boden je Zelle
  lore/ebenen/boeden/<id>.png      ein Graustufenbild je Layer

Aufruf aus dem Projektordner:  python tools/boeden.py
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
import yaml

import bodenfeuchte
import ebenen
import fluesse
import klimazonen
from gestein import read_names

log = logging.getLogger("boeden")

ROOT = Path(__file__).resolve().parent.parent
SRC_SOILS = ROOT / "data" / "nature" / "boeden.yaml"
SRC_MATERIALS = ROOT / "data" / "nature" / "materialien.yaml"
SRC_KOEPPEN_TABLE = ROOT / "data" / "world" / "koeppen.yaml"
SRC_ROOM_TABLE = ROOT / "data" / "world" / "gesteinsraeume.yaml"
SRC_HEIGHT = ROOT / "lore" / "Version 5 HM.png"
OUT_NAME = "boeden.bin"
OUT_MAGIC = b"TBBO"
OUT_MAIN_IMAGE = ROOT / "lore" / "hauptboden.png"
OUT_MOISTURE_IMAGE = ROOT / "lore" / "bodenfeuchte.png"
CATEGORY = "boeden"
MAIN_LAYER = "hauptboden"

HEIGHT_SCALE_M = 8849.0       # welt_konfiguration.md Abschnitt 4, Export
# Annahmen (welt_konfiguration.md Abschnitt 11, O - Vorschlag, nach dem ersten Bild abgestimmt):
CONDITION_SHARE = 0.25        # KALK bzw. SAND: Gesteinsanteil ab diesem Wert
BASIN_MIN_DEPTH_M = 10.0      # SENKE: Zelle liegt so tief unter dem Überlauf ihrer Senke
FILL_MAX_ITERATIONS = 5000
BASIN_SALT_SHARE = 0.5        # Salzboden in Senken der Trockenklimate
# Flussauen je Flussklasse: (Anteil der Flusszelle, Breite des Streifens in km). Breiter als der UFER-Streifen der
# Pflanzen, damit die großen Flüsse fruchtbare Täler bekommen (O - Vorschlag).
FLOODPLAIN_BANDS = {1: (0.40, 30.0), 2: (0.60, 60.0), 3: (0.80, 100.0)}
DRY_RIVER_SAND_SHARE = 0.3    # Sandboden entlang von Trockenflüssen
COAST_FLAT_SLOPE = 0.002      # flache Küste: Gefälle darunter
COAST_SOIL_SHARE = 0.3        # Salz- bzw. Sandboden an flachen Küsten
# Fels: Anteil steigt von 0 bei ROCK_SLOPE_FROM bis ROCK_MAX_SHARE bei ROCK_SLOPE_FULL. Die Pixel der Heightmap sind
# 4,9 km breit, das mittlere Gefälle bleibt daher klein (99 % des Landes unter 2,6 %, Hochgebirge im Mittel 1,2 %).
ROCK_SLOPE_FROM = 0.010
ROCK_SLOPE_FULL = 0.030
ROCK_MAX_SHARE = 0.8
# Hochgebirge (Zone 13, ab 2500 m): fester Fels-Anteil, weil die Hangregel steile Hänge auf 4,9 km nicht sieht.
HIGH_MOUNTAIN_ZONE = 13
HIGH_MOUNTAIN_ROCK_SHARE = 0.5
# Sandmeere: Rauschen mit dieser Strukturgröße (Zellen), Sandboden dort als Hauptboden mit diesem Anteil.
DUNE_NOISE_SIGMA = 6.0
DUNE_SHARE = 0.7
NOTABLE_SHARE = 0.10          # Abdeckung eines Bodens = Fläche, auf der er mindestens so viel Anteil hat
CONDITIONS = {"SENKE", "KALK", "SAND", "FEUCHT"}


def read_rule_data(koeppen_ids, rock_ids, room_ids):
    """boeden.yaml lesen und alle Verweise prüfen; unbekannte IDs brechen ab."""
    if not SRC_SOILS.is_file():
        raise FileNotFoundError(f"Quelle fehlt: {SRC_SOILS}")
    data = yaml.safe_load(SRC_SOILS.read_text(encoding="utf-8")) or {}
    soils = data.get("soils") or []
    soil_ids = [s["id"] for s in soils]
    if len(set(soil_ids)) != len(soil_ids):
        raise ValueError(f"{SRC_SOILS.name}: doppelte Boden-ID")

    def soil(value, where):
        if value not in soil_ids:
            raise ValueError(f"{SRC_SOILS.name}: {where}: unbekannter Boden {value!r}")
        return value

    def codes(values, where):
        for code in values or []:
            if code not in koeppen_ids:
                raise ValueError(f"{SRC_SOILS.name}: {where}: unbekannte Köppen-Klasse {code!r}")
        return list(values or [])

    def condition(value, where):
        if value not in CONDITIONS:
            raise ValueError(f"{SRC_SOILS.name}: {where}: unbekannte Bedingung {value!r}")

    for entry in data["zonal"]["classes"]:
        codes(entry["koeppen"], "zonal")
        soil(entry["main"], "zonal")
        if entry.get("main_if"):
            soil(entry["main_if"]["soil"], "zonal.main_if")
            condition(entry["main_if"]["where"], "zonal.main_if")
        for secondary in entry["secondary"]:
            soil(secondary["soil"], "zonal")
            if secondary.get("where"):
                condition(secondary["where"], "zonal")
    for entry in data.get("transitions") or []:
        soil(entry["soil"], "transitions")
        codes(entry["from"], "transitions")
        codes(entry["to"], "transitions")
    overrides = data["overrides"]
    for entry in overrides["rock"]:
        soil(entry["soil"], "overrides.rock")
        codes(entry.get("koeppen"), "overrides.rock")
        codes(entry.get("exclude_koeppen"), "overrides.rock")
        for rock in entry["rocks"]:
            if rock not in rock_ids:
                raise ValueError(f"{SRC_SOILS.name}: overrides.rock: {rock!r} ist kein Gesteinsanteil")
        if entry.get("moisture") and entry["moisture"] not in bodenfeuchte.MOISTURE_CATEGORIES:
            raise ValueError(f"{SRC_SOILS.name}: overrides.rock: unbekannte Feuchte {entry['moisture']!r}")
        for key in (entry.get("rooms") or []) + (entry.get("exclude_rooms") or []):
            if key not in room_ids:
                raise ValueError(f"{SRC_SOILS.name}: overrides.rock: unbekannter Gesteinsraum {key!r}")
    dunes = overrides["dunes"]
    soil(dunes["soil"], "overrides.dunes")
    codes(dunes["koeppen"], "overrides.dunes")
    if not 0 < dunes["cover"] <= 100:
        raise ValueError(f"{SRC_SOILS.name}: overrides.dunes: cover {dunes['cover']!r} liegt nicht in (0, 100]")
    water, coast = overrides["water"], overrides["coast"]
    for key in ("floodplain", "wetland", "wetland_cold", "basin_salt", "dry_river"):
        soil(water[key], f"overrides.water.{key}")
    codes(water["wetland_cold_koeppen"], "overrides.water")
    codes(water["salt_koeppen"], "overrides.water")
    for key in ("salt", "sand"):
        soil(coast[key], f"overrides.coast.{key}")
    codes(coast["salt_koeppen"], "overrides.coast")
    soil(overrides["slope"], "overrides.slope")
    soil(overrides["high_mountains"], "overrides.high_mountains")
    soil(overrides["ice"]["soil"], "overrides.ice")
    codes(overrides["ice"]["koeppen"], "overrides.ice")
    log.info("Böden geladen: %d Böden, %d zonale Einträge", len(soils), len(data["zonal"]["classes"]))
    return soils, data


def read_rock_share_ids():
    if not SRC_MATERIALS.is_file():
        raise FileNotFoundError(f"Quelle fehlt: {SRC_MATERIALS}")
    data = yaml.safe_load(SRC_MATERIALS.read_text(encoding="utf-8")) or {}
    return [m["id"] for m in data.get("materials") or [] if m.get("rule", {}).get("kind") == "share"]


def read_terrain(width, height):
    """Höhe (Mittel) und Hangneigung (mittleres Gefälle der Heightmap-Pixel, m/m) auf dem Zielraster."""
    if not SRC_HEIGHT.is_file():
        raise FileNotFoundError(f"Quelle fehlt: {SRC_HEIGHT}")
    raw = np.array(Image.open(SRC_HEIGHT)).astype(np.float32) / 65535.0 * HEIGHT_SCALE_M
    rows, cols = raw.shape
    pixel_m = ebenen.PLANET_RADIUS_KM * 1000.0 * np.pi / rows
    lat = np.radians(90.0 - (np.arange(rows) + 0.5) * 180.0 / rows)
    grad_y = np.gradient(raw, axis=0) / pixel_m
    # Ost-West umlaufend; die Pixelbreite schrumpft mit cos(Breite)
    grad_x = (np.roll(raw, -1, axis=1) - np.roll(raw, 1, axis=1)) / 2.0
    grad_x /= pixel_m * np.maximum(np.cos(lat), 0.05)[:, None]
    slope = np.hypot(grad_y, grad_x).astype(np.float32)
    resize = lambda values: np.array(Image.fromarray(values, mode="F").resize((width, height), Image.BOX))
    return resize(raw), resize(slope)


def fill_depth(elevation, ocean):
    """Tiefe unter dem Überlauf der eigenen Senke (Füllen von der Küste her, morphologische Rekonstruktion)."""
    pad = 1
    level = np.where(ocean, elevation, np.inf)
    for iteration in range(FILL_MAX_ITERATIONS):
        wrapped = np.concatenate([level[:, -pad:], level, level[:, :pad]], axis=1)
        lowest = ndimage.grey_erosion(wrapped, size=(3, 3), mode="nearest")[:, pad:-pad]
        updated = np.where(ocean, elevation, np.maximum(elevation, np.minimum(level, lowest)))
        if np.array_equal(updated, level):
            log.info("Senken gefüllt nach %d Durchläufen", iteration + 1)
            break
        level = updated
    else:
        raise RuntimeError(f"Senken nach {FILL_MAX_ITERATIONS} Durchläufen nicht gefüllt")
    return np.where(np.isfinite(level), level - elevation, 0.0)


def set_share(shares, soil, weight, where=None):
    """Setzt soil auf den Anteil weight (0–1); die übrigen Böden schrumpfen im selben Verhältnis."""
    weight = np.clip(weight, 0.0, 1.0)
    if where is not None:
        weight = np.where(where, weight, 0.0)
    for key in shares:
        shares[key] = shares[key] * (1.0 - weight)
    shares[soil] = shares[soil] + weight
    return weight


def log_rule(name, soil, weight, land, area):
    cover = ebenen.coverage_percent(weight > 0, land, area)
    log.info("Regel %s → %s: %.2f %% der Landfläche, mittlerer Anteil dort %.1f %%", name, soil, cover,
             float(weight[weight > 0].mean() * 100.0) if (weight > 0).any() else 0.0)


def zonal(shares, data, grids, moisture):
    main_share = data["zonal"]["main_share"] / 100.0
    conditions = {"SENKE": grids.basin, "KALK": grids.limestone >= CONDITION_SHARE,
                  "SAND": grids.sandstone >= CONDITION_SHARE, "FEUCHT": moisture["feucht"] >= 0.5}
    covered = np.zeros(grids.land.shape, bool)
    for entry in data["zonal"]["classes"]:
        mask = grids.land & np.isin(grids.koeppen_codes, entry["koeppen"])
        covered |= mask
        secondary = entry["secondary"]
        main = main_share if secondary else 1.0
        # main_if: an Stellen mit der Bedingung tritt ein anderer Boden an die Stelle des Hauptbodens
        main_soil = np.full(mask.shape, entry["main"], object)
        if entry.get("main_if"):
            main_soil[conditions[entry["main_if"]["where"]]] = entry["main_if"]["soil"]
        for soil_id in sorted(set(main_soil[mask].tolist())):
            own = mask & (main_soil == soil_id)
            shares[soil_id] += np.where(own, main, 0.0)
            for item in secondary:
                part = (1.0 - main) / len(secondary)
                condition = conditions[item["where"]] if item.get("where") else np.ones(mask.shape, bool)
                shares[item["soil"]] += np.where(own & condition, part, 0.0)
                shares[soil_id] += np.where(own & ~condition, part, 0.0)
    missing = grids.land & ~covered
    if missing.any():
        raise ValueError(f"{SRC_SOILS.name}: Köppen-Klassen ohne zonalen Eintrag: "
                         f"{sorted(set(grids.koeppen_codes[missing].tolist()))}")
    for entry in data.get("transitions") or []:
        side_from = grids.land & np.isin(grids.koeppen_codes, entry["from"])
        side_to = grids.land & np.isin(grids.koeppen_codes, entry["to"])
        half = entry["width_km"] / 2.0
        strip = (side_from & (ebenen.wrapped_distance_km(side_to) <= half)) | \
                (side_to & (ebenen.wrapped_distance_km(side_from) <= half))
        log_rule("Übergang", entry["soil"], set_share(shares, entry["soil"], main_share, strip), grids.land, grids.area)


def rock_overrides(shares, data, grids, moisture):
    dominant = np.argmax(np.stack([grids.rocks[r] for r in grids.rock_ids]), axis=0)
    for entry in data["overrides"]["rock"]:
        indices = [grids.rock_ids.index(r) for r in entry["rocks"]]
        mask = grids.land & np.isin(dominant, indices)
        if entry.get("koeppen"):
            mask &= np.isin(grids.koeppen_codes, entry["koeppen"])
        if entry.get("exclude_koeppen"):
            mask &= ~np.isin(grids.koeppen_codes, entry["exclude_koeppen"])
        if entry.get("rooms"):
            mask &= np.isin(grids.room_keys, entry["rooms"])
        if entry.get("exclude_rooms"):
            mask &= ~np.isin(grids.room_keys, entry["exclude_rooms"])
        if entry.get("moisture") == "FEUCHT":
            mask &= moisture["feucht"] >= 0.5
        elif entry.get("moisture"):
            raise ValueError(f"overrides.rock: Feuchte {entry['moisture']!r} wird von tools/boeden.py nicht ausgewertet")
        weight = sum(grids.rocks[r].astype(np.float64) / 255.0 for r in entry["rocks"]) * entry["strength"]
        log_rule("Gestein " + "+".join(entry["rocks"]), entry["soil"], set_share(shares, entry["soil"], weight, mask),
                 grids.land, grids.area)


def other_overrides(shares, data, grids, moisture):
    land, area, overrides = grids.land, grids.area, data["overrides"]
    # Windablagerungen (Abschnitt 6.3 Regel 2). TODO: Löss braucht die Windrichtung (T-039).
    dunes = overrides["dunes"]
    log_rule("Sandmeer", dunes["soil"], set_share(shares, dunes["soil"], DUNE_SHARE, dune_mask(dunes, grids)), land, area)
    water = overrides["water"]
    log_rule("Flussaue", water["floodplain"], set_share(shares, water["floodplain"],
                                                         bodenfeuchte.river_band(grids.rivers, FLOODPLAIN_BANDS), land),
             land, area)
    cold = np.isin(grids.koeppen_codes, water["wetland_cold_koeppen"])
    log_rule("Feuchtgebiet", water["wetland"], set_share(shares, water["wetland"], moisture["wetland"], land & ~cold),
             land, area)
    log_rule("Feuchtgebiet kühl", water["wetland_cold"],
             set_share(shares, water["wetland_cold"], moisture["wetland"], land & cold), land, area)
    # O - Enden von Trockenflüssen (welt_konfiguration.md Abschnitt 11): alle Senken laufen über, bis Endseen
    # entschieden sind, kommt Salzboden im Landesinneren nur aus den gefüllten Senken der Trockenklimate.
    basin_salt = land & grids.basin & np.isin(grids.koeppen_codes, water["salt_koeppen"])
    log_rule("Senke trocken", water["basin_salt"], set_share(shares, water["basin_salt"], BASIN_SALT_SHARE, basin_salt),
             land, area)
    dry_river = land & (grids.rivers == bodenfeuchte.DRY_RIVER_CLASS)
    log_rule("Trockenfluss", water["dry_river"],
             set_share(shares, water["dry_river"], DRY_RIVER_SAND_SHARE, dry_river), land, area)

    coast = overrides["coast"]
    coastal = land & (ebenen.wrapped_distance_km(grids.ocean) <= grids.cell_km * 1.5) & (grids.slope < COAST_FLAT_SLOPE)
    salty = coastal & np.isin(grids.koeppen_codes, coast["salt_koeppen"])
    log_rule("Küste trocken", coast["salt"], set_share(shares, coast["salt"], COAST_SOIL_SHARE, salty), land, area)
    sandy = coastal & ~salty & ((grids.sandstone >= CONDITION_SHARE) | grids.basin_room)
    log_rule("Sandküste", coast["sand"], set_share(shares, coast["sand"], COAST_SOIL_SHARE, sandy), land, area)

    rock = ROCK_MAX_SHARE * np.clip((grids.slope - ROCK_SLOPE_FROM) / (ROCK_SLOPE_FULL - ROCK_SLOPE_FROM), 0.0, 1.0)
    log_rule("Hang", overrides["slope"], set_share(shares, overrides["slope"], rock, land), land, area)
    log_rule("Hochgebirge", overrides["high_mountains"],
             set_share(shares, overrides["high_mountains"], HIGH_MOUNTAIN_ROCK_SHARE, land & grids.high_mountains),
             land, area)

    ice = land & (grids.ice_room | np.isin(grids.koeppen_codes, overrides["ice"]["koeppen"]))
    log_rule("Eis", overrides["ice"]["soil"], set_share(shares, overrides["ice"]["soil"], 1.0, ice), land, area)


def dune_mask(dunes, grids):
    """Sandmeere: die höchsten Werte eines geglätteten Rauschens, bis cover Prozent der Klassenfläche bedeckt sind."""
    region = grids.land & np.isin(grids.koeppen_codes, dunes["koeppen"])
    if not region.any():
        log.warning("Sandmeer: keine Zelle in %s", dunes["koeppen"])
        return region
    noise = ebenen.smooth_noise(region.shape, DUNE_NOISE_SIGMA, ebenen.seeded_rng("sandmeer_" + dunes["soil"]))
    values, weights = noise[region], grids.area[region]
    order = np.argsort(values)[::-1]
    cut = int(np.searchsorted(np.cumsum(weights[order]) / weights.sum() * 100.0, dunes["cover"]))
    return region & (noise >= values[order[min(cut, len(order) - 1)]])


def original_codes(final_codes, codes):
    """Köppen-Code je Zelle direkt aus dem Bild der Klimasimulation, vor den Regeln von tools/klimazonen.py.
    Wo das Bild Meer zeigt, die Meermaske aber Land, gilt die endgültige Klasse."""
    if not klimazonen.SRC_CLIMATE.is_file():
        raise FileNotFoundError(f"Quelle fehlt: {klimazonen.SRC_CLIMATE}")
    height, width = final_codes.shape
    image = Image.open(klimazonen.SRC_CLIMATE).convert("RGB").resize((width, height), Image.NEAREST)
    zones, numbers = klimazonen.classify(np.array(image), klimazonen.load_class_numbers())
    return np.where(np.isin(zones, klimazonen.LAND_ZONES), codes[numbers], final_codes)


def room_keys(rooms, room_ids):
    """Schlüssel des Gesteinsraums je Zelle (\"\" = Wasser)."""
    keys = np.empty(256, object)
    keys[:] = ""
    for key, number in room_ids.items():
        keys[number] = key
    return keys[rooms]


def load_grids():
    zones = ebenen.read_raster("klimazonen.bin", b"TBKZ")
    height, width = zones.shape
    ocean = np.isin(zones, list(ebenen.WATER_ZONES - {ebenen.INLAND_WATER_ZONE}))
    lake = zones == ebenen.INLAND_WATER_ZONE
    land = ~ocean & ~lake
    koeppen_ids = read_names(SRC_KOEPPEN_TABLE, "code")
    codes = np.empty(256, object)
    codes[:] = ""
    for code, number in koeppen_ids.items():
        codes[number] = code
    final_codes = codes[ebenen.read_raster("koeppen.bin", b"TBKP")]
    room_ids = read_names(SRC_ROOM_TABLE, "key")
    rooms = ebenen.read_raster("gesteinsraeume.bin", b"TBGR")
    rock_ids = read_rock_share_ids()
    rocks = ebenen.read_layer_file("gestein.bin", b"TBGS")
    for rock in rock_ids:
        if rock not in rocks:
            raise ValueError(f"gestein.bin: Layer {rock!r} fehlt (zuerst tools/gestein.py ausführen)")
    elevation, slope = read_terrain(width, height)
    depth = fill_depth(elevation, ocean)
    grids = SimpleNamespace(
        land=land, lake=lake, ocean=ocean, area=ebenen.cell_area_km2(height, width),
        cell_km=ebenen.KM_PER_DEG * 180.0 / height,
        koeppen_codes=final_codes, original_codes=original_codes(final_codes, codes),
        rivers=ebenen.read_raster("fluesse.bin", b"TBFL"),
        slope=slope, basin=land & (depth >= BASIN_MIN_DEPTH_M),
        rock_ids=rock_ids, rocks=rocks,
        limestone=(rocks["kalkstein"].astype(np.float64) + rocks["marmor"]) / 255.0,
        sandstone=rocks["sandstein"].astype(np.float64) / 255.0,
        basin_room=rooms == room_ids["BECKEN"], ice_room=rooms == room_ids["EISSCHILD"],
        room_ids=room_ids, room_keys=room_keys(rooms, room_ids), high_mountains=zones == HIGH_MOUNTAIN_ZONE,
        soil_water=fluesse.read_climate(fluesse.CLIMATE_DATA / "soil_water_index", width, height),
        rain=fluesse.read_climate(fluesse.CLIMATE_DATA / "rainfall_annual", width, height),
        tides=ebenen.read_raster("gezeiten.bin", b"TBGZ"),
    )
    log.info("Senken ab %.0f m Tiefe: %.2f %% der Landfläche", BASIN_MIN_DEPTH_M,
             ebenen.coverage_percent(grids.basin, land, grids.area))
    return grids, koeppen_ids


def build_soils(grids, koeppen_ids, moisture):
    soils, data = read_rule_data(koeppen_ids, grids.rock_ids, grids.room_ids)
    soil_ids = [s["id"] for s in soils]
    shares = {key: np.zeros(grids.land.shape) for key in soil_ids}
    zonal(shares, data, grids, moisture)
    rock_overrides(shares, data, grids, moisture)
    other_overrides(shares, data, grids, moisture)
    total = sum(shares.values())
    if np.abs(total[grids.land] - 1.0).max() > 1e-6:
        raise ValueError("Bodenanteile ergeben nicht überall 1")
    layers = ebenen.quantize_shares(shares, soil_ids, grids.land)
    stack = np.stack([layers[key] for key in soil_ids])
    main = np.where(grids.land, np.argmax(stack, axis=0) + 1, 0).astype(np.uint8)
    for soil in soils:
        values = layers[soil["id"]]
        cover = ebenen.coverage_percent(values >= NOTABLE_SHARE * 255, grids.land, grids.area)
        dominant = ebenen.coverage_percent(main == soil_ids.index(soil["id"]) + 1, grids.land, grids.area)
        if not values.any():
            log.warning("%s: leerer Layer", soil["id"])
        log.info("%s: Abdeckung ab %d %% Anteil %.2f %%, vorherrschend %.2f %%, mittlerer Anteil %.1f %%",
                 soil["id"], NOTABLE_SHARE * 100, cover, dominant, float(values[grids.land].mean() / 2.55))
    return [(key, layers[key]) for key in soil_ids] + [(MAIN_LAYER, main)], soils


def write_soil_images(layers, soils, land, lake):
    by_id = dict(layers)
    palette = np.array([ebenen.SEA_COLOR] + [s["color"] for s in soils], np.uint8)
    image = palette[by_id[MAIN_LAYER]]
    image = np.where(lake[..., None], np.array(bodenfeuchte.LAKE_COLOR, np.uint8), image)
    area = ebenen.cell_area_km2(*land.shape)
    shares = [(ebenen.coverage_percent(by_id[MAIN_LAYER] == i + 1, land, area), s) for i, s in enumerate(soils)]
    entries = [(s["color"], f"{s['id']}  {percent:.1f} %") for percent, s in sorted(shares, key=lambda e: -e[0])
               if percent > 0]
    ebenen.with_legend(image, "Vorherrschender Boden", entries).save(OUT_MAIN_IMAGE)
    log.info("Bild geschrieben: %s", OUT_MAIN_IMAGE)
    ebenen.ensure_layer_image_dir()
    for layer_id, values in layers:
        if layer_id != MAIN_LAYER:
            ebenen.write_layer_image(CATEGORY, layer_id, values, land)
    log.info("Einzelbilder geschrieben: %s (%d)", ebenen.LAYER_IMAGE_DIR / CATEGORY, len(layers) - 1)


def main():
    sys.stderr.reconfigure(encoding="utf-8")
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(funcName)s: %(message)s")
    started = time.perf_counter()
    log.info("Start Bodenfeuchte und Böden")
    try:
        grids, koeppen_ids = load_grids()
        moisture_layers, moisture = bodenfeuchte.build(grids)
        ebenen.write_layer_file(bodenfeuchte.OUT_NAME, bodenfeuchte.OUT_MAGIC, moisture_layers)
        bodenfeuchte.write_images(moisture_layers, grids.land, grids.lake, OUT_MOISTURE_IMAGE)
        soil_layers, soils = build_soils(grids, koeppen_ids, moisture)
        ebenen.write_layer_file(OUT_NAME, OUT_MAGIC, soil_layers)
        write_soil_images(soil_layers, soils, grids.land, grids.lake)
    except Exception:
        log.exception("Bodenfeuchte und Böden konnten nicht erzeugt werden")
        return 1
    log.info("Ende Bodenfeuchte und Böden nach %.1f s", time.perf_counter() - started)
    return 0


if __name__ == "__main__":
    sys.exit(main())
