"""Seifenlagerstätten: schwere Minerale flussabwärts anreichern (T-082); wird von tools/lagerstaetten.py aufgerufen.

Regeln: lore/welt_konfiguration.md Abschnitt 10 (Seifenlagerstätten). Die Fließrichtung kommt aus
data/world/fliessrichtung.bin (tools/fluesse.py): 0 = Meer, 1–8 = Nachbar aus fluesse.NEIGHBOURS, 9 = Mündung.
"""

import logging

import numpy as np

import ebenen
from fluesse import DIRECTION_MOUTH, DIRECTION_SEA, NEIGHBOURS
from klimazonen import lat_of_rows

log = logging.getLogger("seifen")

# Annahmen (welt_konfiguration.md Abschnitt 10, O - Vorschlag):
DECAY_PER_100_KM = 0.5        # Anteil, der nach 100 km Flussstrecke noch ankommt
DRY_RIVER_FACTOR = 0.5        # Trockenflüsse transportieren weniger
DRY_RIVER_CLASS = 4
STEEP_SLOPE = 0.010           # Gefälle (m/m), ab dem ein Abschnitt steil ist ...
FLAT_SLOPE = 0.002            # ... und unter dem die folgende Strecke flach ist
FLAT_BONUS = 1.5              # Anreicherung, wo ein steiler Abschnitt in eine Flachstrecke übergeht


class FlowNetwork:
    """Abflussnetz der Landzellen in Reihenfolge von der Quelle zur Mündung (je Stufe alle Zellen, deren Zuflüsse fertig
    sind). Binnengewässer und Meer beenden den Transport, die Last bleibt in der letzten Landzelle."""

    def __init__(self, directions, rivers, lake, slope, land):
        height, width = directions.shape
        cells = height * width
        lat = np.radians(lat_of_rows(height))
        cell_km_y = ebenen.KM_PER_DEG * 180.0 / height
        cell_km_x = np.broadcast_to((ebenen.KM_PER_DEG * 360.0 / width * np.cos(lat))[:, None], (height, width))
        active = land & ~lake
        self.receiver = np.full(cells, -1, np.int64)
        step_km = np.zeros(cells)
        for direction, (dy, dx) in enumerate(NEIGHBOURS, 1):
            ys, xs = np.nonzero(active & (directions == direction))
            ny = ys + dy
            inside = (ny >= 0) & (ny < height)
            ys, xs, ny = ys[inside], xs[inside], ny[inside]
            index = ys * width + xs
            self.receiver[index] = ny * width + (xs + dx) % width
            step_km[index] = np.hypot(dy * cell_km_y, dx * cell_km_x[ys, xs])
        unknown = active & ~np.isin(directions, list(range(1, 9)) + [DIRECTION_SEA, DIRECTION_MOUTH])
        if unknown.any():
            raise ValueError(f"fliessrichtung.bin: {int(unknown.sum())} Landzellen mit unbekannter Richtung")
        # In Binnengewässer und Meer endet der Transport
        has_target = self.receiver >= 0
        target_active = np.zeros(cells, bool)
        target_active[has_target] = active.ravel()[self.receiver[has_target]]
        self.receiver[has_target & ~target_active] = -1

        factor = DECAY_PER_100_KM ** (step_km / 100.0)
        factor = np.where((rivers.ravel() == DRY_RIVER_CLASS), factor * DRY_RIVER_FACTOR, factor)
        flows = self.receiver >= 0
        bonus = np.ones(cells)
        slope_flat = slope.ravel()
        bonus[flows] = np.where((slope_flat[flows] >= STEEP_SLOPE) & (slope_flat[self.receiver[flows]] < FLAT_SLOPE),
                                FLAT_BONUS, 1.0)
        self.factor = factor * bonus
        self.levels = self._levels(active.ravel())
        self.shape = (height, width)
        log.info("Fließnetz: %d Landzellen, %d Stufen, %d Zellen mit Flachstrecken-Bonus", int(active.sum()),
                 len(self.levels), int((bonus > 1.0).sum()))

    def _levels(self, active):
        """Stufen nach Kahn: zuerst Zellen ohne Zufluss, dann jeweils die, deren Zuflüsse alle verarbeitet sind."""
        flows = self.receiver >= 0
        inflow = np.bincount(self.receiver[flows], minlength=self.receiver.size)
        frontier = np.flatnonzero(active & (inflow == 0))
        levels, processed = [], 0
        while frontier.size:
            levels.append(frontier)
            processed += frontier.size
            targets = self.receiver[frontier]
            targets = targets[targets >= 0]
            if targets.size == 0:
                break
            unique, counts = np.unique(targets, return_counts=True)
            inflow[unique] -= counts
            frontier = unique[inflow[unique] == 0]
        if processed != int(active.sum()):
            raise ValueError(f"Fließnetz enthält Kreise: {int(active.sum()) - processed} Zellen nicht erreicht")
        return levels

    def carry(self, source):
        """Quellwert je Zelle flussabwärts tragen: Wert = eigene Quelle + Σ Zufluss × Abfall je Strecke × Bonus."""
        carried = np.asarray(source, np.float64).ravel().copy()
        for level in self.levels:
            targets = self.receiver[level]
            flows = targets >= 0
            np.add.at(carried, targets[flows], carried[level[flows]] * self.factor[level[flows]])
        return carried.reshape(self.shape)
