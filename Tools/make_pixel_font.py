"""Convierte la fuente bitmap OnLincesPixel (BMFont 5x7) a TTF de contornos cuadrados.

Cada píxel encendido se vuelve un cuadro de 100x100 unidades; con 1100 unidades por em,
TextMeshPro en modo RASTER a 11 pt dibuja 1 píxel de la fuente = 1 píxel del atlas.
Uso: python3 Tools/make_pixel_font.py
"""
import re
from pathlib import Path
from PIL import Image
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen

ROOT = Path(__file__).resolve().parent.parent
FNT = ROOT / "Assets/Fonts/pixel.fnt"
PNG = ROOT / "Assets/Fonts/pixel.png"
OUT = ROOT / "Assets/Fonts/OnLincesPixel.ttf"
U = 100  # unidades por píxel

common = {}
chars = {}
for line in FNT.read_text().splitlines():
    kv = dict(re.findall(r'(\w+)=("[^"]*"|\S+)', line))
    if line.startswith("common"):
        common = {k: int(v) for k, v in kv.items() if v.lstrip("-").isdigit()}
    elif line.startswith("char "):
        chars[int(kv["id"])] = {k: int(v) for k, v in kv.items() if v.lstrip("-").isdigit()}

atlas = Image.open(PNG).convert("RGBA")
base, line_h = common["base"], common["lineHeight"]


def bitmap_of(c):
    rows = []
    for j in range(c["height"]):
        rows.append([atlas.getpixel((c["x"] + i, c["y"] + j))[3] > 127 for i in range(c["width"])])
    return rows


# glifos extra que faltan en la fuente (5x7, alineados a la base)
EXTRA = {
    0x2192: ["......", "...#..", "....#.", "######", "....#.", "...#..", "......"],   # →
    0x2190: ["......", "..#...", ".#....", "######", ".#....", "..#...", "......"],   # ←
    0x00D7: [".....", ".....", "#...#", ".#.#.", "..#..", ".#.#.", "#...#"],          # ×
    0x2022: ["...", "...", "...", ".#.", "###", ".#.", "..."],                        # •
    0x2026: [".....", ".....", ".....", ".....", ".....", ".....", "#.#.#"],          # …
    0x2014: ["......", "......", "......", "######", "......", "......", "......"],   # —
    0x2013: ["....", "....", "....", "####", "....", "....", "...."],                 # –
    0x2018: [".#", "#.", "#.", "..", "..", "..", ".."],                               # ‘
    0x2019: [".#", ".#", "#.", "..", "..", "..", ".."],                               # ’
    0x201C: [".#.#", "#.#.", "#.#.", "....", "....", "....", "...."],                 # “
    0x201D: [".#.#", ".#.#", "#.#.", "....", "....", "....", "...."],                 # ”
    0x00BD: ["#....", "#....", "#..#.", "..#..", ".#.##", "#...#", "...##"],          # ½
    0x00B2: ["##.", "..#", ".#.", "###", "...", "...", "..."],                        # ²
    0x00BA: [".#.", "#.#", ".#.", "###", "...", "...", "..."],                        # º
}

glyphs = {".notdef": (U * 4, [])}
cmap = {}


def squares(rows, x0, top):
    """Píxeles encendidos -> cuadros (x, y_abajo) en unidades de fuente."""
    out = []
    for j, row in enumerate(rows):
        for i, on in enumerate(row):
            if on:
                out.append(((x0 + i) * U, (top - j - 1) * U))
    return out


for cid, c in chars.items():
    rows = bitmap_of(c)
    name = f"uni{cid:04X}"
    glyphs[name] = (c["xadvance"] * U, squares(rows, c["xoffset"], base - c["yoffset"]))
    cmap[cid] = name

for cid, pattern in EXTRA.items():
    rows = [[ch == "#" for ch in r] for r in pattern]
    name = f"uni{cid:04X}"
    glyphs[name] = ((len(pattern[0]) + 1) * U, squares(rows, 0, base - 2))
    cmap[cid] = name

order = list(glyphs)
fb = FontBuilder(unitsPerEm=11 * U, isTTF=True)
fb.setupGlyphOrder(order)
fb.setupCharacterMap(cmap)
pen_glyphs, metrics = {}, {}
for name in order:
    adv, sq = glyphs[name]
    pen = TTGlyphPen(None)
    for x, y in sq:
        pen.moveTo((x, y)); pen.lineTo((x, y + U)); pen.lineTo((x + U, y + U)); pen.lineTo((x + U, y)); pen.closePath()
    pen_glyphs[name] = pen.glyph()
    metrics[name] = (adv, min((x for x, _ in sq), default=0))
fb.setupGlyf(pen_glyphs)
fb.setupHorizontalMetrics(metrics)
asc, desc = base * U, (line_h - base) * U
fb.setupHorizontalHeader(ascent=asc, descent=-desc)
fb.setupNameTable({"familyName": "OnLincesPixel", "styleName": "Regular"})
fb.setupOS2(sTypoAscender=asc, sTypoDescender=-desc, sTypoLineGap=0, usWinAscent=asc, usWinDescent=desc)
fb.setupPost()
fb.save(OUT)
print("ok", OUT, len(cmap), "caracteres")
