"""Bodenfeuchte, Feuchtgebietsanteil und Wasserkategorien (T-078); wird von tools/boeden.py aufgerufen.

Regeln: lore/welt_konfiguration.md Abschnitt 11, Bedeutung der Wasserkategorien: lore/pflanzen.md Abschnitt 3.
Ergebnis (über tools/boeden.py):
  data/world/bodenfeuchte.bin          Ebenen-Datei: bodenfeuchte, feuchtgebiet und je Wasserkategorie ein Anteil
  lore/bodenfeuchte.png                vorherrschende Feuchtekategorie je Zelle
  lore/ebenen/bodenfeuchte/<id>.png    ein Graustufenbild je Layer
"""

import logging

import numpy as np
from scipy import ndimage

import ebenen

log = logging.getLogger("bodenfeuchte")

OUT_NAME = "bodenfeuchte.bin"
OUT_MAGIC = b"TBBF"
CATEGORY = "bodenfeuchte"

# Annahmen (welt_konfiguration.md Abschnitt 11, O - Vorschlag, nach dem ersten Bild abgestimmt):
# Bodenfeuchte = Gewicht × Bodenwasser-Index + Rest × Regenindex; Regenindex = 1 − exp(−Regen / Skala).
# Der Index allein unterschätzt die wechselfeuchten Tropen (Aw im Mittel 0,23 bei 1160 mm Regen).
SOIL_WATER_WEIGHT = 0.7
RAIN_SCALE_MM = 1000.0
DRY_LIMIT = 0.15              # darunter TROCKEN
WET_LIMIT = 0.55              # darüber FEUCHT, dazwischen FRISCH
CATEGORY_BAND = 0.05          # weicher Übergang ± um die Grenzen
WETLAND_FROM = 0.55           # Feuchtgebiet ab dieser Bodenfeuchte ...
WETLAND_FULL = 0.85           # ... voll ab dieser
FLAT_SLOPE = 0.004            # Gefälle (m/m, Mittel der Heightmap-Pixel), ab dem kein Feuchtgebiet mehr entsteht
WETLAND_RIVER_KM = 40.0       # Abklingweite der Flussnähe (ganzjährige Flüsse)
WETLAND_LAKE_KM = 30.0        # Abklingweite der Seenähe
# Ganzjährige Flüsse je Klasse: (Anteil der Flusszelle, Breite des Streifens in km). Gilt für UFER und Flussauen.
RIVER_BANDS = {1: (0.25, 10.0), 2: (0.40, 25.0), 3: (0.55, 50.0)}
DRY_RIVER_CLASS = 4
LAKE_SHORE_SHARE = 0.4        # UFER in Zellen direkt am Binnengewässer, klingt über LAKE_SHORE_KM ab
LAKE_SHORE_KM = 30.0
OASIS_SHARE = 0.4             # OASE an Trockenflüssen und an Flüssen in B-Klimaten, klingt über OASIS_KM ab
OASIS_KM = 20.0
COAST_SHARE = 0.4             # KUESTE in Zellen direkt am Meer, nach einer weiteren Zelle 0
ARID_KOEPPEN = {"BWh", "BWk", "BSh", "BSk"}
# Wo eine Regel der Klimazonen die Köppen-Klasse geändert hat (vor allem „Südkette grüner“), passen Bodenwasser und
# Regen der Klimasimulation nicht mehr zur Klasse. Dort wird die Bodenfeuchte um den Unterschied der typischen Werte
# (Median über die unveränderten Zellen) verschoben und weich auslaufen gelassen (O - Vorschlag).
CLASS_SHIFT_SIGMA = 3.0       # Glättung der Verschiebung, in Zellen

MOISTURE_CATEGORIES = ["TROCKEN", "FRISCH", "FEUCHT", "NASS"]
# TODO: GEZEITEN (welt_konfiguration.md Abschnitt 11): Quelle offen, die Gezeiten liegen nur als Bild vor
# (07-tides-raw.png, Lauf 2026-10-10). Bis dahin gibt es den Layer nicht.
PLACE_CATEGORIES = ["UFER", "OASE", "KUESTE", "STEHEND"]
CATEGORY_COLORS = {"TROCKEN": (225, 195, 130), "FRISCH": (130, 185, 90), "FEUCHT": (40, 130, 90), "NASS": (40, 80, 120)}
LAKE_COLOR = (85, 140, 210)


def river_band(rivers, bands=RIVER_BANDS):
    """Anteil des Streifens entlang ganzjähriger Flüsse (größter Wert über alle Klassen); bands wie RIVER_BANDS."""
    band = np.zeros(rivers.shape)
    for river_class, (share, width_km) in bands.items():
        mask = rivers == river_class
        if not mask.any():
            continue
        distance = ebenen.wrapped_distance_km(mask)
        band = np.maximum(band, share * np.clip(1.0 - distance / width_km, 0.0, 1.0))
    return band


def align_to_classes(moisture, final_codes, original_codes, land):
    """Verschiebt die Bodenfeuchte geänderter Zellen um Median(neue Klasse) − Median(alte Klasse)."""
    changed = land & (final_codes != original_codes)
    reference = land & ~changed
    medians = {code: float(np.median(moisture[reference & (final_codes == code)]))
               for code in np.unique(final_codes[land]) if (reference & (final_codes == code)).any()}
    delta = np.zeros(moisture.shape)
    for final_code, original_code in sorted(set(zip(final_codes[changed].tolist(), original_codes[changed].tolist()))):
        if final_code not in medians or original_code not in medians:
            log.warning("Keine unveränderten Zellen für %s bzw. %s, Bodenfeuchte bleibt", final_code, original_code)
            continue
        delta[changed & (final_codes == final_code) & (original_codes == original_code)] =             medians[final_code] - medians[original_code]
    delta = ndimage.gaussian_filter(delta, CLASS_SHIFT_SIGMA, mode=("nearest", "wrap"))
    log.info("Bodenfeuchte an geänderte Köppen-Klassen angeglichen: %d Zellen, mittlere Verschiebung dort %+.3f",
             int(changed.sum()), float(delta[changed].mean()) if changed.any() else 0.0)
    return np.where(land, np.clip(moisture + delta, 0.0, 1.0), 0.0)


def soft_categories(moisture):
    """TROCKEN, FRISCH, FEUCHT als weiche Anteile (Summe 1)."""
    dry = np.clip((DRY_LIMIT + CATEGORY_BAND - moisture) / (2 * CATEGORY_BAND), 0.0, 1.0)
    wet = np.clip((moisture - WET_LIMIT + CATEGORY_BAND) / (2 * CATEGORY_BAND), 0.0, 1.0)
    return dry, np.clip(1.0 - dry - wet, 0.0, 1.0), wet


def build(grids):
    """grids: Raster aus tools/boeden.py (land, lake, ocean, koeppen_codes, original_codes, rivers, slope, soil_water,
    rain, cell_km, area).
    Ergebnis: Liste (ID, uint8-Layer) und dict mit den Werten 0–1, die die Böden brauchen."""
    land, lake, ocean, cell_km = grids.land, grids.lake, grids.ocean, grids.cell_km
    rain_index = 1.0 - np.exp(-np.maximum(grids.rain, 0.0) / RAIN_SCALE_MM)
    moisture = np.where(land, np.clip(SOIL_WATER_WEIGHT * grids.soil_water + (1 - SOIL_WATER_WEIGHT) * rain_index,
                                      0.0, 1.0), 0.0)
    moisture = align_to_classes(moisture, grids.koeppen_codes, grids.original_codes, land)

    perennial = np.isin(grids.rivers, list(RIVER_BANDS))
    near_river = np.exp(-ebenen.wrapped_distance_km(perennial) / WETLAND_RIVER_KM) if perennial.any() else 0.0
    lake_distance = ebenen.wrapped_distance_km(lake)
    near_water = np.maximum(near_river, np.exp(-lake_distance / WETLAND_LAKE_KM))
    wet_factor = np.clip((moisture - WETLAND_FROM) / (WETLAND_FULL - WETLAND_FROM), 0.0, 1.0)
    flat_factor = np.clip(1.0 - grids.slope / FLAT_SLOPE, 0.0, 1.0)
    wetland = np.where(land, wet_factor * flat_factor * near_water, 0.0)

    dry, fresh, wet = soft_categories(moisture)
    rest = 1.0 - wetland
    shares = {"TROCKEN": dry * rest, "FRISCH": fresh * rest, "FEUCHT": wet * rest, "NASS": wetland}
    quantized = ebenen.quantize_shares(shares, MOISTURE_CATEGORIES, land)

    rivers_band = river_band(grids.rivers)
    lake_shore = LAKE_SHORE_SHARE * np.clip(1.0 - (lake_distance - cell_km) / LAKE_SHORE_KM, 0.0, 1.0)
    arid = np.isin(grids.koeppen_codes, list(ARID_KOEPPEN))
    oasis_source = (grids.rivers == DRY_RIVER_CLASS) | (perennial & arid)
    oasis = OASIS_SHARE * np.clip(1.0 - ebenen.wrapped_distance_km(oasis_source) / OASIS_KM, 0.0, 1.0) \
        if oasis_source.any() else np.zeros(land.shape)
    coast = COAST_SHARE * np.clip(1.0 - (ebenen.wrapped_distance_km(ocean) - cell_km) / cell_km, 0.0, 1.0)
    places = {
        "UFER": np.where(land, np.maximum(rivers_band, lake_shore), 0.0),
        "OASE": np.where(land, oasis, 0.0),
        "KUESTE": np.where(land, coast, 0.0),
        "STEHEND": lake.astype(np.float64),
    }

    layers = [("bodenfeuchte", to_byte(moisture)), ("feuchtgebiet", to_byte(wetland))]
    layers += [(key, quantized[key]) for key in MOISTURE_CATEGORIES]
    layers += [(key, to_byte(places[key])) for key in PLACE_CATEGORIES]
    log_layers(layers, land, lake, grids.area)
    values = {"moisture": moisture, "wetland": wetland,
              "feucht": quantized["FEUCHT"].astype(np.float64) / 255.0}
    return layers, values


def to_byte(values):
    return np.clip(np.rint(values * 255.0), 0, 255).astype(np.uint8)


def log_layers(layers, land, lake, area):
    for layer_id, values in layers:
        region = land | lake if layer_id == "STEHEND" else land
        cover = ebenen.coverage_percent(values > 0, region, area)
        if cover == 0:
            log.warning("%s: leerer Layer", layer_id)
        log.info("%s: Abdeckung %.2f %%, Mittel %.1f, Maximum %d", layer_id, cover,
                 float(values[region].mean()), int(values.max()))


def write_images(layers, land, lake, main_image):
    by_id = dict(layers)
    stack = np.stack([by_id[key] for key in MOISTURE_CATEGORIES])
    dominant = np.argmax(stack, axis=0)
    palette = np.array([CATEGORY_COLORS[key] for key in MOISTURE_CATEGORIES], np.uint8)
    image = np.where(land[..., None], palette[dominant], np.array(ebenen.SEA_COLOR, np.uint8))
    image = np.where(lake[..., None], np.array(LAKE_COLOR, np.uint8), image)
    area = ebenen.cell_area_km2(*land.shape)
    entries = [(CATEGORY_COLORS[key], f"{key.capitalize()}  {ebenen.coverage_percent(dominant == i, land, area):.1f} %")
               for i, key in enumerate(MOISTURE_CATEGORIES)]
    entries.append((LAKE_COLOR, "Binnengewässer"))
    ebenen.with_legend(image, "Bodenfeuchte", entries).save(main_image)
    log.info("Bild geschrieben: %s", main_image)
    ebenen.ensure_layer_image_dir()
    for layer_id, values in layers:
        ebenen.write_layer_image(CATEGORY, layer_id, values, land | lake if layer_id == "STEHEND" else land)
    log.info("Einzelbilder geschrieben: %s (%d)", ebenen.LAYER_IMAGE_DIR / CATEGORY, len(layers))
