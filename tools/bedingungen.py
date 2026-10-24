"""Bedingungen der Lagerstätten-Regeln (T-082); wird von tools/lagerstaetten.py genutzt.

Eine Regel ist ein dict aus Bedingungen; jede liefert einen Faktor 0–1 je Zelle, die Faktoren werden multipliziert.
Bedeutung der Schlüssel: Kopf von data/nature/materialien.yaml, Verfahren lore/welt_konfiguration.md Abschnitt 10.
"""

import logging

import numpy as np

import ebenen
from gesteinsraeume import distance_to_polyline_km

log = logging.getLogger("bedingungen")

# Annahme (O - Vorschlag): flach = Gefälle (Mittel der Heightmap-Pixel) darunter, Faktor fällt linear bis dorthin
FLAT_SLOPE = 0.004

CONDITION_KEYS = {"rooms", "source", "sources", "combine", "min_source", "presence", "koeppen", "exclude_koeppen", "soils", "wetland", "flat",
                  "coast_km", "rivers", "lakes_km", "near_rooms", "min_elevation_m", "max_elevation_m", "chains",
                  "chain_km", "weight", "any_of"}


def source_ids(condition):
    """Alle Layer, auf die eine Bedingung (samt any_of) verweist."""
    ids = ([condition["source"]] if "source" in condition else []) + list(condition.get("sources") or [])
    for part in condition.get("any_of") or []:
        ids += source_ids(part)
    return ids


def validate(condition, where, known):
    """Prüft Schlüssel und Verweise; known: dict mit Mengen rooms, koeppen, soils, layers, chains."""
    unknown = set(condition) - CONDITION_KEYS
    if unknown:
        raise ValueError(f"{where}: unbekannte Bedingung {sorted(unknown)}")
    checks = [("rooms", list(condition.get("rooms") or {})), ("rooms", list(condition.get("near_rooms") or {})),
              ("koeppen", (condition.get("koeppen") or []) + (condition.get("exclude_koeppen") or [])),
              ("soils", condition.get("soils") or []),
              ("layers", ([condition["source"]] if "source" in condition else []) + list(condition.get("sources") or [])),
              ("chains", condition.get("chains") or [])]
    for kind, values in checks:
        for value in values:
            if value not in known[kind]:
                raise ValueError(f"{where}: unbekannter Verweis {value!r} ({kind})")
    if condition.get("combine", "any") not in ("any", "all"):
        raise ValueError(f"{where}: combine {condition['combine']!r} ist weder any noch all")
    if "chains" in condition and "chain_km" not in condition:
        raise ValueError(f"{where}: chains braucht chain_km")
    for index, part in enumerate(condition.get("any_of") or []):
        validate(part, f"{where}.any_of[{index}]", known)


class Conditions:
    """Wertet Bedingungen auf den Rastern aus; Abstände werden je Ziel einmal gerechnet."""

    def __init__(self, grids, chains):
        self.grids = grids
        self.chains = chains
        self._room_km = {}
        self._chain_km = {}
        height, width = grids.land.shape
        self.lon = np.broadcast_to((-180.0 + (np.arange(width) + 0.5) * 360.0 / width)[None, :], (height, width))
        self.lat = np.broadcast_to((90.0 - (np.arange(height) + 0.5) * 180.0 / height)[:, None], (height, width))

    def room_km(self, room):
        if room not in self._room_km:
            self._room_km[room] = ebenen.wrapped_distance_km(self.grids.room_keys == room)
        return self._room_km[room]

    def chain_km(self, chain_id):
        if chain_id not in self._chain_km:
            self._chain_km[chain_id] = distance_to_polyline_km(self.chains[chain_id]["points"], self.lon, self.lat)
        return self._chain_km[chain_id]

    def factor(self, condition, layers):
        g = self.grids
        result = np.ones(g.land.shape)
        if "rooms" in condition:
            result *= sum(g.room_weights[room] * weight for room, weight in condition["rooms"].items())
        ids = ([condition["source"]] if "source" in condition else []) + list(condition.get("sources") or [])
        if ids:
            minimum = condition.get("min_source", 0.0)
            parts = []
            for layer_id in ids:
                values = layers[layer_id].astype(np.float64) / 255.0
                kept = (values >= minimum) & (values > 0)
                # presence: nur ob die Quelle da ist zählt (1), nicht ihr Wert
                parts.append(kept.astype(np.float64) if condition.get("presence") else np.where(kept, values, 0.0))
            combine = np.minimum.reduce if condition.get("combine", "any") == "all" else np.maximum.reduce
            result *= combine(parts)
        if "koeppen" in condition:
            result *= np.isin(g.koeppen_codes, condition["koeppen"])
        if "exclude_koeppen" in condition:
            result *= ~np.isin(g.koeppen_codes, condition["exclude_koeppen"])
        if "soils" in condition:
            result *= sum(g.soils[soil].astype(np.float64) for soil in condition["soils"]) / 255.0
        if condition.get("wetland"):
            result *= g.wetland
        if condition.get("flat"):
            result *= np.clip(1.0 - g.slope / FLAT_SLOPE, 0.0, 1.0)
        if "coast_km" in condition:
            result *= g.ocean_km <= condition["coast_km"]
        if condition.get("rivers"):
            result *= g.river_band > 0
        if "lakes_km" in condition:
            result *= g.lake_km <= condition["lakes_km"]
        if "near_rooms" in condition:
            result *= np.logical_or.reduce([self.room_km(room) <= km for room, km in condition["near_rooms"].items()])
        if "min_elevation_m" in condition:
            result *= g.elevation >= condition["min_elevation_m"]
        if "max_elevation_m" in condition:
            result *= g.elevation < condition["max_elevation_m"]
        if "chains" in condition:
            result *= np.logical_or.reduce([self.chain_km(c) <= condition["chain_km"] for c in condition["chains"]])
        if "weight" in condition:
            result *= condition["weight"]
        if condition.get("any_of"):
            result *= np.maximum.reduce([self.factor(part, layers) for part in condition["any_of"]])
        return result
