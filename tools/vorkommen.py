"""Gemeinsames Verfahren der Vorkommen (Gestein T-077, Lagerstätten T-082).

Rauschen je Verteilung, Kalibrierung auf die Zielabdeckung und Reihenfolge nach Abhängigkeiten.
Regeln: lore/welt_konfiguration.md Abschnitt 10.
"""

import logging

import numpy as np
from scipy import ndimage

import ebenen

log = logging.getLogger("vorkommen")

# Annahmen (welt_konfiguration.md Abschnitt 10, O - Werte nach dem ersten Bild):
NOISE_SIGMA = {"FLAECHIG": 10.0, "KLUMPIG": 3.0, "ABGELEITET": 3.0}
POINT_DENSITY = 0.002         # PUNKTUELL: Anteil der Zellen, die Zentrum eines Fundorts sind
POINT_SIGMA = 1.5             # Radius eines Fundorts, in Zellen
MAX_RECORDED_SHORTFALL = 2.0  # Warnung, wenn die Abdeckung um mehr als diesen Faktor vom Ziel abweicht


def distribution_noise(distribution, shape, rng):
    if distribution == "PUNKTUELL":
        centers = (rng.random(shape) < POINT_DENSITY).astype(np.float32) * rng.random(shape)
        field = ndimage.gaussian_filter(centers, POINT_SIGMA, mode=("nearest", "wrap"))
        return field / max(field.max(), 1e-9)
    return np.exp(ebenen.smooth_noise(shape, NOISE_SIGMA[distribution], rng))


def calibrate(material_id, value, target, land, area):
    """Schwellwert so, dass die flächengewichtete Abdeckung der Zellen mit Wert > 0 dem Ziel (% des Landes) entspricht.
    Ergebnis: Layer (über dem Schwellwert 1–255, 255 = höchster Wert), geeignete Fläche in %, Schwellwert."""
    candidates = value > 0
    possible = ebenen.coverage_percent(candidates, land, area)
    if not candidates.any():
        log.warning("%s: keine geeignete Zelle", material_id)
        return np.zeros(land.shape, np.uint8), 0.0, 0.0
    # Schwellwert: flächengewichtetes Quantil der Werte, sodass die Zielabdeckung übrig bleibt
    values, weights_area = value[candidates], area[candidates]
    order = np.argsort(values)[::-1]
    cumulative = np.cumsum(weights_area[order]) / area[land].sum() * 100.0
    cut = int(np.searchsorted(cumulative, target))
    threshold = values[order[min(cut, len(order) - 1)]]
    kept = value >= threshold
    peak = value.max()
    scaled = np.where(kept, 1.0 + (value - threshold) / max(peak - threshold, 1e-9) * 254.0, 0.0)
    return np.clip(np.rint(scaled), 0, 255).astype(np.uint8), possible, float(threshold)


def log_result(material, values, target, possible, threshold, land, area):
    """Logzeile je Vorkommen und Warnung bei leerer Karte oder Abweichung über MAX_RECORDED_SHORTFALL."""
    cover = ebenen.coverage_percent(values > 0, land, area)
    log.info("%s (%s): Abdeckung %.3f %% (Ziel %.3f %%, geeignet %.2f %%), Schwellwert %.4g, Flecken %d",
             material["id"], material["distribution"], cover, target, possible, threshold,
             ndimage.label(values > 0)[1])
    if cover == 0 or max(cover / target, target / max(cover, 1e-9)) > MAX_RECORDED_SHORTFALL:
        log.warning("%s: Abdeckung %.3f %% weicht um mehr als Faktor %g vom Ziel %.3f %% ab",
                    material["id"], cover, MAX_RECORDED_SHORTFALL, target)
    return cover


def topological(materials, dependencies):
    """Reihenfolge nach Abhängigkeiten (Quelle vor Abgeleitetem); Zyklen brechen ab.
    dependencies(material) liefert die IDs, von denen es abhängt; IDs außerhalb von materials zählen nicht."""
    by_id = {m["id"]: m for m in materials}
    done, order, visiting = set(), [], set()

    def visit(material_id):
        if material_id in done:
            return
        if material_id in visiting:
            raise ValueError(f"Zyklus in den Abhängigkeiten bei {material_id}")
        visiting.add(material_id)
        for source in dependencies(by_id[material_id]):
            if source in by_id:
                visit(source)
        visiting.discard(material_id)
        done.add(material_id)
        order.append(by_id[material_id])

    for material in materials:
        visit(material["id"])
    return order
