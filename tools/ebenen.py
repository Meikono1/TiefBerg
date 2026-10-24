"""Gemeinsame Hilfen der Natur-Ebenen (Gestein, Böden, Lagerstätten, Pflanzen; T-077).

Formate und Regeln: lore/welt_konfiguration.md Abschnitt 8.
  Einzelraster (klimazonen.bin, koeppen.bin, gesteinsraeume.bin …): Kennung, uint32 Version, Breite, Höhe, ein Byte je Zelle.
  Ebenen-Datei (gestein.bin …): Kennung, uint32 Version, Breite, Höhe, Anzahl Layer, je Layer ID (uint16 Länge + UTF-8)
  und Länge der gepackten Daten, danach die Layer, je ein Byte je Zelle, einzeln mit zlib gepackt.
"""

import logging
import struct
import zlib
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

from klimazonen import lat_of_rows

log = logging.getLogger("ebenen")

ROOT = Path(__file__).resolve().parent.parent
WORLD_DIR = ROOT / "data" / "world"
LAYER_IMAGE_DIR = ROOT / "lore" / "ebenen"

FORMAT_VERSION = 1
PLANET_RADIUS_KM = 6371.0
KM_PER_DEG = PLANET_RADIUS_KM * np.pi / 180.0
WATER_ZONES = {20, 21, 22, 23, 24, 25}
INLAND_WATER_ZONE = 25
SEA_COLOR = (30, 40, 60)

# Fester Startwert der Welt (welt_konfiguration.md Abschnitt 8); je Eintrag mit der ID kombiniert.
WORLD_SEED = 1


def read_raster(name, magic):
    """Einzelraster aus data/world lesen und prüfen."""
    path = WORLD_DIR / name
    if not path.is_file():
        raise FileNotFoundError(f"Quelle fehlt: {path}")
    data = path.read_bytes()
    if data[:4] != magic:
        raise ValueError(f"{name}: Kennung {data[:4]!r} statt {magic!r}")
    version, width, height = struct.unpack("<III", data[4:16])
    if version != FORMAT_VERSION or len(data) != 16 + width * height:
        raise ValueError(f"{name}: Version {version} oder Größe passt nicht")
    return np.frombuffer(data, np.uint8, offset=16).reshape(height, width)


def write_layer_file(name, magic, layers):
    """Schreibt eine Ebenen-Datei; layers ist eine Liste (ID, uint8-Array) in fester Reihenfolge."""
    if not layers:
        raise ValueError(f"{name}: keine Layer")
    height, width = layers[0][1].shape
    packed = [(layer_id.encode("utf-8"), zlib.compress(np.ascontiguousarray(values, np.uint8).tobytes(), 9))
              for layer_id, values in layers]
    path = WORLD_DIR / name
    WORLD_DIR.mkdir(parents=True, exist_ok=True)
    with open(path, "wb") as file:
        file.write(magic)
        file.write(struct.pack("<IIII", FORMAT_VERSION, width, height, len(packed)))
        for key, data in packed:
            file.write(struct.pack("<H", len(key)))
            file.write(key)
            file.write(struct.pack("<I", len(data)))
        for _, data in packed:
            file.write(data)
    log.info("Ebenen geschrieben: %s (%d Layer, %.1f MB)", path, len(packed), path.stat().st_size / 1e6)


def read_layer_file(name, magic):
    """Liest eine Ebenen-Datei; Ergebnis: dict ID → uint8-Array (Reihenfolge wie in der Datei)."""
    path = WORLD_DIR / name
    if not path.is_file():
        raise FileNotFoundError(f"Quelle fehlt: {path}")
    data = path.read_bytes()
    if data[:4] != magic:
        raise ValueError(f"{name}: Kennung {data[:4]!r} statt {magic!r}")
    version, width, height, count = struct.unpack("<IIII", data[4:20])
    if version != FORMAT_VERSION:
        raise ValueError(f"{name}: Version {version} wird nicht unterstützt")
    offset, entries = 20, []
    for _ in range(count):
        (key_length,) = struct.unpack("<H", data[offset:offset + 2])
        key = data[offset + 2:offset + 2 + key_length].decode("utf-8")
        (length,) = struct.unpack("<I", data[offset + 2 + key_length:offset + 6 + key_length])
        entries.append((key, length))
        offset += 6 + key_length
    layers = {}
    for key, length in entries:
        layers[key] = np.frombuffer(zlib.decompress(data[offset:offset + length]), np.uint8).reshape(height, width)
        offset += length
    return layers


def quantize_shares(shares, ids, land):
    """Anteile (Summe 1 je Landzelle) auf ganze Bytes mit Summe 255 je Landzelle (größte Reste zuerst)."""
    stack = np.stack([shares[key] for key in ids]) * 255.0
    floor = np.floor(stack)
    remainder = stack - floor
    missing = (255 - floor.sum(axis=0)).astype(int)
    order = np.argsort(-remainder, axis=0)
    rank = np.empty_like(order)
    np.put_along_axis(rank, order, np.arange(len(ids))[:, None, None], axis=0)
    result = floor + (rank < missing[None]).astype(np.float64)
    result[:, ~land] = 0
    return {key: result[i].astype(np.uint8) for i, key in enumerate(ids)}


def cell_area_km2(height, width):
    lat = np.radians(lat_of_rows(height))
    area = PLANET_RADIUS_KM ** 2 * (2 * np.pi / width) * (np.pi / height) * np.cos(lat)
    return np.broadcast_to(area[:, None], (height, width))


def coverage_percent(mask, land, area):
    """Flächengewichteter Anteil der Landfläche in %."""
    return float(area[mask & land].sum() / area[land].sum() * 100.0)


def seeded_rng(entry_id):
    return np.random.default_rng([WORLD_SEED, zlib.crc32(entry_id.encode("utf-8"))])


def smooth_noise(shape, sigma, rng):
    """Geglättetes Rauschen mit Mittelwert 0 und Standardabweichung 1, Ost-West umlaufend."""
    noise = ndimage.gaussian_filter(rng.standard_normal(shape), sigma, mode=("nearest", "wrap"))
    return (noise - noise.mean()) / max(noise.std(), 1e-9)


def wrapped_distance_km(mask_from):
    """Ungefährer Abstand zur nächsten True-Zelle in km (Zellhöhe als Maß, Ost-West umlaufend)."""
    height, width = mask_from.shape
    pad = width // 4
    padded = np.concatenate([mask_from[:, -pad:], mask_from, mask_from[:, :pad]], axis=1)
    cell_km = KM_PER_DEG * 180.0 / height
    return ndimage.distance_transform_edt(~padded)[:, pad:pad + width] * cell_km


def write_layer_image(category, layer_id, values, land):
    """Graustufenbild eines Layers: Meer dunkelblau, Land von dunkelgrau (0) bis weiß (255)."""
    folder = LAYER_IMAGE_DIR / category
    folder.mkdir(parents=True, exist_ok=True)
    gray = (60 + values.astype(np.float32) * (195.0 / 255.0)).astype(np.uint8)
    image = np.where(land[..., None], np.repeat(gray[..., None], 3, axis=2), np.array(SEA_COLOR, np.uint8))
    Image.fromarray(image.astype(np.uint8)).save(folder / f"{layer_id}.png")


LEGEND_WIDTH = 380
LEGEND_FONT = "arial.ttf"


def with_legend(image, title, entries):
    """Hängt rechts eine Legende an: Titel, dann je Eintrag Farbfeld und Text. entries: Liste (Farbe, Text)."""
    height, width = image.shape[:2]
    sheet = Image.new("RGB", (width + LEGEND_WIDTH, height), SEA_COLOR)
    sheet.paste(Image.fromarray(image.astype(np.uint8)), (0, 0))
    draw = ImageDraw.Draw(sheet)
    title_font = ImageFont.truetype(LEGEND_FONT, 30)
    font = ImageFont.truetype(LEGEND_FONT, 24)
    x, y = width + 24, 30
    draw.text((x, y), title, fill=(240, 240, 240), font=title_font)
    y += 60
    row = min(44, (height - y - 20) // max(len(entries), 1))
    for color, text in entries:
        draw.rectangle([x, y, x + 34, y + row - 12], fill=tuple(int(c) for c in color), outline=(230, 230, 230))
        draw.text((x + 48, y + (row - 12) // 2 - 13), text, fill=(235, 235, 235), font=font)
        y += row
    return sheet


def ensure_layer_image_dir():
    """lore/ebenen/ mit .gdignore, damit Godot die vielen Einzelbilder nicht importiert."""
    LAYER_IMAGE_DIR.mkdir(parents=True, exist_ok=True)
    marker = LAYER_IMAGE_DIR / ".gdignore"
    if not marker.exists():
        marker.write_text("", encoding="utf-8")
