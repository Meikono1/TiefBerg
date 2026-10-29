"""Land und Meer im Raster des Spielfelds (T-081): sagt, ob ein gewähltes Feld Land oder Wasser ist.

Regeln: lore/welt_konfiguration.md Abschnitt 8 (Spielfeld), Design-Referenz Abschnitt 4.
Quelle:
  lore/Version5 Meermaske.png      255 = Land, 0 = Meer, 8192 × 4096 (dieselbe Maske wie in tools/klimazonen.py)
Ergebnis:
  data/world/landmaske.bin         Ebenen-Datei mit einem Layer „land“ (255 = Land, 0 = Meer), 8192 × 4096
  lore/landmaske.png               Land und Meer
Binnengewässer kennt die Maske nicht; sie liegen nur im Raster der Klimazonen.

Aufruf aus dem Projektordner:  python tools/landmaske.py
Benötigt numpy, Pillow.
"""

import logging
import sys
import time

import numpy as np
from PIL import Image

import ebenen

log = logging.getLogger("landmaske")

SRC_MASK = ebenen.ROOT / "lore" / "Version5 Meermaske.png"
OUT_NAME = "landmaske.bin"
OUT_MAGIC = b"TBLM"
OUT_IMAGE = ebenen.ROOT / "lore" / "landmaske.png"
WIDTH, HEIGHT = 8192, 4096
LAND_COLOR = (95, 95, 95)


def build():
    if not SRC_MASK.is_file():
        raise FileNotFoundError(f"Quelle fehlt: {SRC_MASK}")
    image = Image.open(SRC_MASK).convert("L")
    if image.size != (WIDTH, HEIGHT):
        raise ValueError(f"{SRC_MASK.name}: {image.size[0]} × {image.size[1]} statt {WIDTH} × {HEIGHT}")
    land = np.array(image) > 127
    area = ebenen.cell_area_km2(HEIGHT, WIDTH)
    log.info("Land: %.1f %% der Fläche (%.1f %% der Pixel)", float(area[land].sum() / area.sum() * 100.0),
             float(land.mean() * 100.0))
    return land


def write_image(land):
    image = np.where(land[..., None], np.array(LAND_COLOR, np.uint8), np.array(ebenen.SEA_COLOR, np.uint8))
    Image.fromarray(image.astype(np.uint8)).save(OUT_IMAGE)
    log.info("Bild geschrieben: %s", OUT_IMAGE)


def main():
    sys.stderr.reconfigure(encoding="utf-8")
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(funcName)s: %(message)s")
    started = time.perf_counter()
    log.info("Start Landmaske")
    try:
        land = build()
        ebenen.write_layer_file(OUT_NAME, OUT_MAGIC, [("land", np.where(land, 255, 0).astype(np.uint8))])
        write_image(land)
    except Exception:
        log.exception("Landmaske konnte nicht erzeugt werden")
        return 1
    log.info("Fertig in %.1f s", time.perf_counter() - started)
    return 0


if __name__ == "__main__":
    sys.exit(main())
