"""Liest und prüft die Pflanzen- und Regionsdaten für tools/pflanzen.py (T-079).

Felder der Pflanzen: Kopf von data/nature/pflanzen.yaml, Abbildung der Standort-Angaben: lore/pflanzen.md Abschnitt 2.
Unbekannte Werte (Köppen-Klasse, Boden, Wasserkategorie, Wuchsform, Nutzung) brechen ab, statt geraten zu werden
(welt_konfiguration.md Abschnitt 8).
"""

import logging
from pathlib import Path
from types import SimpleNamespace

import numpy as np
from PIL import Image
import yaml

log = logging.getLogger("pflanzen_daten")

ROOT = Path(__file__).resolve().parent.parent
SRC_PLANTS = ROOT / "data" / "nature" / "pflanzen.yaml"
SRC_SOILS = ROOT / "data" / "nature" / "boeden.yaml"
SRC_REGIONS = ROOT / "lore" / "regionen.yaml"
SRC_REGION_LINES = ROOT / "lore" / "regionen_grenzen.png"

GROWTH_FORMS = {"BAUM", "STRAUCH", "ZWERGSTRAUCH", "STAUDE", "GRAS", "GETREIDE", "KNOLLE", "HUELSENFRUCHT",
                "KLETTERPFLANZE", "PALME", "SUKKULENTE", "MOOS", "FLECHTE", "WASSERPFLANZE", "ALGE"}
WATER_CATEGORIES = ["TROCKEN", "FRISCH", "FEUCHT", "NASS", "UFER", "OASE", "KUESTE", "GEZEITEN", "STEHEND"]
USE_CODES = {"N", "Fu", "F", "P", "H", "B", "O", "W", "G", "Fa", "S", "M", "T", "I"}
PH_ORDER = ["STARK_SAUER", "SAUER", "SCHWACH_SAUER", "NEUTRAL", "BASISCH"]
FERTILITY_PROFILES = {"FRUCHTBAR", "ARM"}
TERRAINS = {"HANG"}
DRAINAGES = {"GUT"}
PLANT_KEYS = {"id", "name_key", "growth", "climate", "water", "soil", "fertility", "ph", "elevation_m", "salt_tolerant",
              "terrain", "drainage", "frost_free", "island_bonus", "worldwide", "homeland", "line", "shade", "fire_pioneer",
              "fog", "use",
              "fictional", "active"}
SOIL_KEYS = {"prefer", "tolerate", "only", "exclude"}
CLIMATE_CONDITION_KEYS = {"weight", "water", "elevation_m", "coast_km"}
# Wuchsformen im Wasser: Der Boden der Zelle zählt für sie nicht.
SOIL_FREE_FORMS = {"WASSERPFLANZE", "ALGE"}

# Annahmen (welt_konfiguration.md Abschnitt 12, O - Vorschlag): Bodengewicht nach Fruchtbarkeit des Bodens (0–5)
# je Profil der Pflanze. Ohne Angabe meiden Pflanzen arme Böden etwas, ARM verträgt sie, FRUCHTBAR braucht reiche.
FERTILITY_WEIGHTS = {
    None: [0.1, 0.6, 0.8, 1.0, 1.0, 1.0],
    "ARM": [0.1, 1.0, 1.0, 1.0, 1.0, 1.0],
    "FRUCHTBAR": [0.05, 0.3, 0.5, 0.75, 1.0, 1.0],
}
PREFER_OTHERS = 0.6       # nicht bevorzugte Böden, wenn eine Pflanze bevorzugte hat
TOLERATE_MIN = 0.8        # tolerierte Böden mindestens so viel
PH_MISMATCH = 0.3         # pH-Spanne von Boden und Pflanze überschneidet sich nicht
DRAINAGE_EXCLUDE = {"GUT": ["gley", "moor"]}
SALT_SOIL = "salzboden"
ICE_SOIL = "eis"


def _load(path):
    if not path.is_file():
        raise FileNotFoundError(f"Quelle fehlt: {path}")
    return yaml.safe_load(path.read_text(encoding="utf-8")) or {}


def read_soils():
    soils = _load(SRC_SOILS).get("soils") or []
    if not soils:
        raise ValueError(f"{SRC_SOILS.name}: keine Böden")
    return soils


def _check_list(plant_id, field, values, allowed):
    unknown = [v for v in values if v not in allowed]
    if unknown:
        raise ValueError(f"Pflanze {plant_id}: unbekannte Werte in {field}: {unknown}")


def _check_range(plant_id, field, values):
    if not isinstance(values, list) or len(values) != 2:
        raise ValueError(f"Pflanze {plant_id}: {field} braucht [von, bis]")
    return tuple(values)


def _climate(entry, koeppen_ids):
    climate = {}
    for code, value in (entry.get("climate") or {}).items():
        if code not in koeppen_ids:
            raise ValueError(f"Pflanze {entry['id']}: unbekannte Köppen-Klasse {code!r}")
        if isinstance(value, dict):
            _check_list(entry["id"], f"climate.{code}", value, CLIMATE_CONDITION_KEYS)
            _check_list(entry["id"], f"climate.{code}.water", value.get("water") or [], WATER_CATEGORIES)
            condition = {
                "water": value.get("water"),
                "elevation_m": _check_range(entry["id"], "elevation_m", value["elevation_m"]) if "elevation_m" in value else None,
                "coast_km": value.get("coast_km"),
            }
            climate[code] = (float(value.get("weight", 1.0)), condition)
        else:
            climate[code] = (float(value), None)
    if not climate:
        raise ValueError(f"Pflanze {entry['id']}: kein Klima")
    return climate


def _ph_overlaps(plant_ph, soil_ph):
    low, high = (PH_ORDER.index(v) for v in plant_ph)
    soil_low, soil_high = (PH_ORDER.index(v) for v in soil_ph)
    return soil_low <= high and low <= soil_high


def soil_weights(entry, soils):
    """Gewicht je Boden (Reihenfolge wie boeden.yaml) aus Fruchtbarkeit, Vorlieben, pH und Salz."""
    profile = FERTILITY_WEIGHTS[entry.get("fertility")]
    spec = entry.get("soil") or {}
    excluded = set(spec.get("exclude") or []) | set(DRAINAGE_EXCLUDE.get(entry.get("drainage"), []))
    weights = []
    for soil in soils:
        soil_id = soil["id"]
        weight = profile[soil["fertility"]]
        if spec.get("prefer"):
            weight = 1.0 if soil_id in spec["prefer"] else weight * PREFER_OTHERS
        if soil_id in (spec.get("tolerate") or []):
            weight = max(weight, TOLERATE_MIN)
        if spec.get("only") and soil_id not in spec["only"]:
            weight = 0.0
        if soil_id in excluded or soil_id == ICE_SOIL or (soil_id == SALT_SOIL and not entry.get("salt_tolerant")):
            weight = 0.0
        if entry.get("ph") and soil["ph"] and not _ph_overlaps(entry["ph"], soil["ph"]):
            weight *= PH_MISMATCH
        weights.append(weight)
    return np.array(weights, np.float32)


def read_plants(koeppen_ids, soils, regions):
    """Alle Pflanzen geprüft; Ergebnis: Liste von SimpleNamespace in der Reihenfolge der Datei."""
    entries = _load(SRC_PLANTS).get("plants") or []
    soil_ids = {s["id"] for s in soils}
    region_numbers = {region["id"]: number for number, region in enumerate(regions, start=1)}
    plants, seen = [], set()
    for entry in entries:
        plant_id = entry.get("id")
        if not plant_id or plant_id in seen:
            raise ValueError(f"{SRC_PLANTS.name}: fehlende oder doppelte ID {plant_id!r}")
        seen.add(plant_id)
        _check_list(plant_id, "Felder", entry, PLANT_KEYS)
        for field in ("name_key", "growth", "water", "use"):
            if field not in entry:
                raise ValueError(f"Pflanze {plant_id}: Pflichtfeld {field} fehlt")
        _check_list(plant_id, "growth", [entry["growth"]], GROWTH_FORMS)
        _check_list(plant_id, "water", entry["water"], WATER_CATEGORIES)
        _check_list(plant_id, "use", entry["use"], USE_CODES)
        spec = entry.get("soil") or {}
        _check_list(plant_id, "soil", spec, SOIL_KEYS)
        for key in SOIL_KEYS:
            _check_list(plant_id, f"soil.{key}", spec.get(key) or [], soil_ids)
        if entry.get("fertility") is not None:
            _check_list(plant_id, "fertility", [entry["fertility"]], FERTILITY_PROFILES)
        if entry.get("ph") is not None:
            _check_list(plant_id, "ph", _check_range(plant_id, "ph", entry["ph"]), PH_ORDER)
        if entry.get("terrain") is not None:
            _check_list(plant_id, "terrain", [entry["terrain"]], TERRAINS)
        if entry.get("drainage") is not None:
            _check_list(plant_id, "drainage", [entry["drainage"]], DRAINAGES)
        # Verbreitung: entweder weltweit oder Heimat in bestimmten Regionen (Design-Referenz Abschnitt 5)
        if bool(entry.get("worldwide")) == bool(entry.get("homeland")):
            raise ValueError(f"Pflanze {plant_id}: genau eins von worldwide und homeland angeben")
        _check_list(plant_id, "homeland", entry.get("homeland") or [], region_numbers)
        homeland = sorted(region_numbers[r] for r in entry["homeland"]) if entry.get("homeland") else None
        plants.append(SimpleNamespace(
            id=plant_id, entry=entry, growth=entry["growth"], climate=_climate(entry, koeppen_ids),
            water=entry["water"], use=set(entry["use"]), soil_weights=soil_weights(entry, soils),
            elevation_m=_check_range(plant_id, "elevation_m", entry["elevation_m"]) if "elevation_m" in entry else None,
            hang=entry.get("terrain") == "HANG", frost_free=bool(entry.get("frost_free")),
            island_bonus=bool(entry.get("island_bonus")), line=bool(entry.get("line")), homeland=homeland,
            active=entry.get("active", True), soil_free=entry["growth"] in SOIL_FREE_FORMS,
        ))
    log.info("Pflanzen geladen: %d, davon aktiv %d, weltweit %d, mit Linie %d", len(plants),
             sum(p.active for p in plants), sum(p.homeland is None for p in plants), sum(p.line for p in plants))
    return plants


def read_regions():
    regions = _load(SRC_REGIONS).get("regions") or []
    ids = [region.get("id") for region in regions]
    if not regions or None in ids or len(set(ids)) != len(ids):
        raise ValueError(f"{SRC_REGIONS.name}: keine, fehlende oder doppelte IDs")
    for region in regions:
        if len(region.get("point") or []) != 2:
            raise ValueError(f"{SRC_REGIONS.name}: Region {region['id']} braucht point [Länge, Breite]")
    return regions


def read_region_lines(shape):
    """Grenzlinien: deckende Pixel in regionen_grenzen.png, Größe wie die Weltdaten."""
    if not SRC_REGION_LINES.is_file():
        raise FileNotFoundError(f"Quelle fehlt: {SRC_REGION_LINES}")
    image = Image.open(SRC_REGION_LINES)
    if image.size != (shape[1], shape[0]) or image.mode != "RGBA":
        raise ValueError(f"{SRC_REGION_LINES.name}: {image.size} {image.mode} statt {shape[1]} × {shape[0]} RGBA")
    lines = np.array(image)[..., 3] >= 128
    if not lines.any():
        raise ValueError(f"{SRC_REGION_LINES.name}: keine Grenzlinien")
    return lines
