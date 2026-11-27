"""Paneles de UI como en la versión original: fondo #0c0a12 casi opaco, borde #3a3650 de 1 px
con las esquinas recortadas y una línea de acento de 1 px dentro del borde superior.
Uso: python3 Tools/make_ui_panels.py
"""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent / "Assets/UI/Sprites"
BG = (12, 10, 18, 235)
BORDER = (58, 54, 80, 255)
ACCENT = (96, 90, 128, 255)


def panel(w, h, accent=True):
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for y in range(h):
        for x in range(w):
            edge = x in (0, w - 1) or y in (0, h - 1)
            corner = x in (0, w - 1) and y in (0, h - 1)
            if corner:
                continue                      # esquina recortada
            im.putpixel((x, y), BORDER if edge else BG)
    if accent:
        for x in range(2, w - 2):
            im.putpixel((x, 1), ACCENT)       # brillo arriba
    return im


panel(40, 20).save(ROOT / "panel.png")
panel(6, 6, accent=False).save(ROOT / "weapon_panel.png")
print("ok")
