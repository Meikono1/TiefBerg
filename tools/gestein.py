"""Berechnet Grundgestein und Gesteine als Ebenen (T-077).

Regeln: lore/welt_konfiguration.md Abschnitt 10, Inhalte: lore/materialien_und_boeden.md Abschnitte 3 und 5.1.
Quellen:
  data/nature/materialien.yaml     Gesteinsanteile je Gesteinsraum und Regeln je Material (Kategorie GESTEIN, dazu Gips)
  data/world/klimazonen.bin        Land, Meer, Binnengewässer
  data/world/koeppen.bin           Köppen-Klassen (für Gips und Kreide)
  data/world/gesteinsraeume.bin    Gesteinsraum je Zelle (tools/gesteinsraeume.py)
Ergebnis:
  data/world/gestein.bin           Ebenen-Datei: Anteile am Grundgestein (Summe je Landzelle 255) und Vorkommen (0–255)
  lore/hauptgestein.png            vorherrschendes Gestein je Zelle
  lore/ebenen/gestein/<id>.png     ein Graustufenbild je Layer

Aufruf aus dem Projektordner:  python tools/gestein.py
Benötigt numpy, Pillow, scipy, PyYAML.
"""

import logging
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage
import yaml

import ebenen
from klimazonen import label_wrapped

log = logging.getLogger("gestein")

ROOT = Path(__file__).resolve().parent.parent
SRC_MATERIALS = ROOT / "data" / "nature" / "materialien.yaml"
SRC_KOEPPEN_TABLE = ROOT / "data" / "world" / "koeppen.yaml"
SRC_ROOM_TABLE = ROOT / "data" / "world" / "gesteinsraeume.yaml"
OUT_NAME = "gestein.bin"
OUT_MAGIC = b"TBGS"
OUT_MAIN_IMAGE = ROOT / "lore" / "hauptgestein.png"
CATEGORY = "gestein"

# Annahmen (welt_konfiguration.md Abschnitt 10, O - Werte nach dem ersten Bild):
ROOM_BLEND_SIGMA = 1.5        # weiche Übergänge zwischen Gesteinsräumen, in Zellen
SHARE_NOISE_SIGMA = 8.0       # Strukturgröße der Anteilsschwankung, in Zellen
SHARE_NOISE_STRENGTH = 0.5    # Anteil × exp(Stärke × Rauschen)
NOTABLE_SHARE = 0.10          # Abdeckung eines Anteils = Fläche, auf der er mindestens so groß ist
GYPSUM_KOEPPEN = {"BWh", "BWk", "BSh", "BSk"}
GYPSUM_INLAND_WATER_KM = 50.0
DESERT_KOEPPEN = {"BWh", "BWk"}
CONTACT_GRANITE = 0.30        # Kontaktsaum im ALTGEBIRGE: Granit mindestens so viel ...
CONTACT_LIMESTONE = 0.10      # ... und Kalkstein mindestens so viel
CONTACT_TO_MARBLE = 0.5       # Anteil des Kalksteins, der dort zu Marmor wird
NOISE_SIGMA = {"FLAECHIG": 10.0, "KLUMPIG": 3.0, "ABGELEITET": 3.0}
POINT_DENSITY = 0.002         # PUNKTUELL: Anteil der Zellen, die Zentrum eines Fundorts sind
POINT_SIGMA = 1.5             # Radius eines Fundorts, in Zellen
MAX_RECORDED_SHORTFALL = 2.0  # Warnung, wenn die Abdeckung um mehr als diesen Faktor vom Ziel abweicht


def read_names(path, field):
    """ID → Schlüssel bzw. Code aus einer erzeugten Tabelle (koeppen.yaml, gesteinsraeume.yaml)."""
    if not path.is_file():
        raise FileNotFoundError(f"Quelle fehlt: {path}")
    data = yaml.safe_load(path.read_text(encoding="utf-8"))
    entries = data.get("classes") or data.get("types") or []
    return {entry[field]: entry["id"] for entry in entries}


def read_materials(room_ids):
    if not SRC_MATERIALS.is_file():
        raise FileNotFoundError(f"Quelle fehlt: {SRC_MATERIALS}")
    data = yaml.safe_load(SRC_MATERIALS.read_text(encoding="utf-8")) or {}
    targets = data.get("rarity_classes") or {}
    shares = data.get("rock_shares") or {}
    materials = [m for m in data.get("materials") or [] if m.get("rule", {}).get("kind") in ("share", "occurrence")]
    ids = {m["id"] for m in materials}
    share_ids = [m["id"] for m in materials if m["rule"]["kind"] == "share"]
    for room, table in shares.items():
        if room not in room_ids:
            raise ValueError(f"{SRC_MATERIALS.name}: unbekannter Gesteinsraum {room!r} in rock_shares")
        for rock in table:
            if rock not in share_ids:
                raise ValueError(f"{SRC_MATERIALS.name}: {rock!r} in rock_shares ist kein Material mit rule.kind share")
    for material in materials:
        if material.get("rarity") not in targets:
            raise ValueError(f"{SRC_MATERIALS.name}: {material['id']}: unbekannte Seltenheit {material.get('rarity')!r}")
        rule = material["rule"]
        for room in list((rule.get("rooms") or {}).keys()) + list(rule.get("source_rooms") or []):
            if room not in room_ids:
                raise ValueError(f"{SRC_MATERIALS.name}: {material['id']}: unbekannter Gesteinsraum {room!r}")
        if rule.get("source") and rule["source"] not in ids:
            raise ValueError(f"{SRC_MATERIALS.name}: {material['id']}: Quelle {rule['source']!r} fehlt")
        if rule["kind"] == "occurrence" and material.get("distribution") not in set(NOISE_SIGMA) | {"PUNKTUELL"}:
            raise ValueError(f"{SRC_MATERIALS.name}: {material['id']}: Verteilung {material.get('distribution')!r} "
                             "wird von tools/gestein.py nicht erzeugt")
    log.info("Materialien geladen: %d Anteile, %d Vorkommen", len(share_ids), len(materials) - len(share_ids))
    return materials, shares, targets


def room_weights(rooms, land, room_ids):
    """Weiches Gewicht je Gesteinsraum (Summe 1 je Landzelle), damit Gesteine an den Grenzen ineinander übergehen."""
    land_weight = ndimage.gaussian_filter(land.astype(np.float32), ROOM_BLEND_SIGMA, mode=("nearest", "wrap"))
    weights = {}
    for key, room_id in room_ids.items():
        mask = (rooms == room_id).astype(np.float32)
        weights[key] = np.where(land, ndimage.gaussian_filter(mask, ROOM_BLEND_SIGMA, mode=("nearest", "wrap"))
                                / np.maximum(land_weight, 1e-6), 0.0)
    total = sum(weights.values())
    return {key: np.where(land, w / np.maximum(total, 1e-6), 0.0) for key, w in weights.items()}


def base_shares(weights, shares, share_ids, land):
    """Anteile aus der Tabelle, weich über die Gesteinsräume gemischt und mit Rauschen verschoben (Summe 1)."""
    result = {}
    for rock in share_ids:
        value = sum(weights[room] * table.get(rock, 0) / 100.0 for room, table in shares.items())
        noise = ebenen.smooth_noise(land.shape, SHARE_NOISE_SIGMA, ebenen.seeded_rng("anteil_" + rock))
        result[rock] = value * np.exp(SHARE_NOISE_STRENGTH * noise)
    total = sum(result.values())
    return {rock: np.where(land, value / np.maximum(total, 1e-9), 0.0) for rock, value in result.items()}


def move_share(shares, source, target, mask, fraction=1.0):
    moved = np.where(mask, shares[source] * fraction, 0.0)
    shares[source] = shares[source] - moved
    shares[target] = shares[target] + moved
    return float(moved.sum())


def apply_conditions(shares, koeppen, koeppen_ids, rooms, room_ids, zones, land):
    """Klimabedingungen und Kontaktsaum (materialien_und_boeden.md Abschnitt 3)."""
    dry = np.isin(koeppen, [koeppen_ids[c] for c in GYPSUM_KOEPPEN])
    near_lake = ebenen.wrapped_distance_km(zones == ebenen.INLAND_WATER_ZONE) <= GYPSUM_INLAND_WATER_KM
    no_gypsum = land & ~dry & ~near_lake
    log.info("Gips → Kalkstein außerhalb BW/BS und fern von Binnengewässern: %d Zellen",
             int((no_gypsum & (shares["gips"] > 0)).sum()))
    move_share(shares, "gips", "kalkstein", no_gypsum)

    # Kreide nur in Becken, die nicht ausschließlich Wüste sind; jede Zelle zählt zum nächsten Becken
    basin = rooms == room_ids["BECKEN"]
    labels, count = label_wrapped(basin)
    desert = np.isin(koeppen, [koeppen_ids[c] for c in DESERT_KOEPPEN])
    cells = np.bincount(labels.ravel(), minlength=count + 1)
    desert_cells = np.bincount(labels.ravel(), weights=desert.ravel(), minlength=count + 1)
    all_desert = (cells > 0) & (desert_cells >= cells)
    all_desert[0] = False
    if basin.any():
        _, (iy, ix) = ndimage.distance_transform_edt(~basin, return_indices=True)
        no_chalk = land & all_desert[labels[iy, ix]]
    else:
        no_chalk = land.copy()
    log.info("Kreide → Kalkstein in reinen Wüstenbecken: %d von %d Becken", int(all_desert.sum()), count)
    move_share(shares, "kreide", "kalkstein", no_chalk)

    contact = (rooms == room_ids["ALTGEBIRGE"]) & (shares["granit"] >= CONTACT_GRANITE) & (shares["kalkstein"] >= CONTACT_LIMESTONE)
    log.info("Kontaktsaum Kalkstein/Granit im Altgebirge → Marmor: %d Zellen", int(contact.sum()))
    move_share(shares, "kalkstein", "marmor", contact, CONTACT_TO_MARBLE)


def distribution_noise(distribution, shape, rng):
    if distribution == "PUNKTUELL":
        centers = (rng.random(shape) < POINT_DENSITY).astype(np.float32) * rng.random(shape)
        field = ndimage.gaussian_filter(centers, POINT_SIGMA, mode=("nearest", "wrap"))
        return field / max(field.max(), 1e-9)
    return np.exp(ebenen.smooth_noise(shape, NOISE_SIGMA[distribution], rng))


def occurrence(material, layers, weights, target, land, area, ice):
    """Vorkommen: Eignung × Rauschen, Schwellwert so, dass die Abdeckung dem Ziel entspricht (Kalibrierung)."""
    rule = material["rule"]
    source = None
    if rule.get("source"):
        source = layers[rule["source"]].astype(np.float32) / 255.0
        source = np.where(source >= rule.get("min_source", 0.0), source, 0.0)
    rooms = rule.get("rooms")
    if rooms:
        source_rooms = set(rule.get("source_rooms") or rooms)
        suitability = sum(weights[room] * factor * (source if source is not None and room in source_rooms else 1.0)
                          for room, factor in rooms.items())
    else:
        suitability = source if source is not None else np.ones(land.shape)
    rng = ebenen.seeded_rng(material["id"])
    value = np.where(land & ~ice, suitability * distribution_noise(material["distribution"], land.shape, rng), 0.0)
    candidates = value > 0
    possible = ebenen.coverage_percent(candidates, land, area)
    if not candidates.any():
        log.warning("%s: keine geeignete Zelle", material["id"])
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


def topological(materials):
    """Reihenfolge nach Abhängigkeiten (Quelle vor Abgeleitetem); Zyklen brechen ab."""
    by_id = {m["id"]: m for m in materials}
    done, order, visiting = set(), [], set()

    def visit(material_id):
        if material_id in done:
            return
        if material_id in visiting:
            raise ValueError(f"Zyklus in den Abhängigkeiten bei {material_id}")
        visiting.add(material_id)
        source = by_id[material_id]["rule"].get("source")
        if source:
            visit(source)
        visiting.discard(material_id)
        done.add(material_id)
        order.append(by_id[material_id])

    for material in materials:
        visit(material["id"])
    return order


def build():
    zones = ebenen.read_raster("klimazonen.bin", b"TBKZ")
    land = ~np.isin(zones, list(ebenen.WATER_ZONES))
    koeppen = ebenen.read_raster("koeppen.bin", b"TBKP")
    rooms = ebenen.read_raster("gesteinsraeume.bin", b"TBGR")
    koeppen_ids = read_names(SRC_KOEPPEN_TABLE, "code")
    room_ids = read_names(SRC_ROOM_TABLE, "key")
    materials, shares_table, targets = read_materials(room_ids)
    area = ebenen.cell_area_km2(*land.shape)
    ice = rooms == room_ids["EISSCHILD"]

    weights = room_weights(rooms, land, room_ids)
    share_ids = [m["id"] for m in materials if m["rule"]["kind"] == "share"]
    shares = base_shares(weights, shares_table, share_ids, land)
    apply_conditions(shares, koeppen, koeppen_ids, rooms, room_ids, zones, land)
    layers = ebenen.quantize_shares(shares, share_ids, land)
    if (sum(layers[r].astype(int) for r in share_ids)[land] != 255).any():
        raise ValueError("Gesteinsanteile ergeben nicht überall 255")

    for material in topological(materials):
        target = targets[material["rarity"]]
        if material["rule"]["kind"] == "share":
            cover = ebenen.coverage_percent(layers[material["id"]] >= NOTABLE_SHARE * 255, land, area)
            log.info("%s (Anteil): Abdeckung ab %d %% Anteil %.2f %% (Zielklasse %s, %.2f %%), mittlerer Anteil %.1f %%",
                     material["id"], NOTABLE_SHARE * 100, cover, material["rarity"], target,
                     float(layers[material["id"]][land].mean() / 2.55))
            continue
        values, possible, threshold = occurrence(material, layers, weights, target, land, area, ice)
        layers[material["id"]] = values
        cover = ebenen.coverage_percent(values > 0, land, area)
        log.info("%s (%s): Abdeckung %.3f %% (Ziel %.3f %%, geeignet %.2f %%), Schwellwert %.4g, Flecken %d",
                 material["id"], material["distribution"], cover, target, possible, threshold,
                 ndimage.label(values > 0)[1])
        if cover == 0 or max(cover / target, target / max(cover, 1e-9)) > MAX_RECORDED_SHORTFALL:
            log.warning("%s: Abdeckung %.3f %% weicht um mehr als Faktor %g vom Ziel %.3f %% ab",
                        material["id"], cover, MAX_RECORDED_SHORTFALL, target)
    return [(m["id"], layers[m["id"]]) for m in materials], materials, share_ids, land


def write_images(layers, materials, share_ids, land):
    colors = {m["id"]: m["color"] for m in materials}
    by_id = dict(layers)
    stack = np.stack([by_id[rock] for rock in share_ids])
    palette = np.array([colors[rock] for rock in share_ids], np.uint8)
    dominant = np.argmax(stack, axis=0)
    image = np.where(land[..., None], palette[dominant], np.array(ebenen.SEA_COLOR, np.uint8))
    # Legende: Gesteine nach Fläche, auf der sie vorherrschen (Name = ID mit großem Anfangsbuchstaben)
    area = ebenen.cell_area_km2(*land.shape)
    shares = [(ebenen.coverage_percent(dominant == i, land, area), rock) for i, rock in enumerate(share_ids)]
    entries = [(colors[rock], f"{rock.capitalize()}  {percent:.1f} %") for percent, rock in sorted(shares, reverse=True)
               if percent > 0]
    ebenen.with_legend(image, "Vorherrschendes Gestein", entries).save(OUT_MAIN_IMAGE)
    log.info("Bild geschrieben: %s", OUT_MAIN_IMAGE)
    ebenen.ensure_layer_image_dir()
    for layer_id, values in layers:
        ebenen.write_layer_image(CATEGORY, layer_id, values, land)
    log.info("Einzelbilder geschrieben: %s (%d)", ebenen.LAYER_IMAGE_DIR / CATEGORY, len(layers))


def main():
    sys.stderr.reconfigure(encoding="utf-8")
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(funcName)s: %(message)s")
    try:
        layers, materials, share_ids, land = build()
        ebenen.write_layer_file(OUT_NAME, OUT_MAGIC, layers)
        write_images(layers, materials, share_ids, land)
    except Exception:
        log.exception("Gestein konnte nicht erzeugt werden")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
