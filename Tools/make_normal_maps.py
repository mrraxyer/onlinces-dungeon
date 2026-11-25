"""Genera <sprite>_n.png (normal map) para los sprites del juego.

Altura = brillo del píxel mezclado con un bisel desde el borde transparente,
así el centro de cada figura "sobresale" y la luz 2D le da volumen.
Uso: python Tools/make_normal_maps.py   (desde la raíz del proyecto)
"""
import math
import pathlib
from collections import deque

from PIL import Image

ROOT = pathlib.Path("Assets/Art/Sprites")
FOLDERS = ["Bosses", "Characters", "Enemies", "Items", "Props", "Tiles", "Weapons"]
BEVEL_PX = 3
STRENGTH = 2.5


def bevel(alpha, w, h):
    """Distancia (en px, máx BEVEL_PX) al píxel transparente más cercano."""
    dist = [[BEVEL_PX] * w for _ in range(h)]
    q = deque()
    for y in range(h):
        for x in range(w):
            if alpha[y][x] == 0:
                dist[y][x] = 0
                q.append((x, y))
    while q:
        x, y = q.popleft()
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and dist[ny][nx] > dist[y][x] + 1:
                    dist[ny][nx] = dist[y][x] + 1
                    q.append((nx, ny))
    return dist


def normal_map(src):
    im = Image.open(src).convert("RGBA")
    w, h = im.size
    px = im.load()
    alpha = [[px[x, y][3] for x in range(w)] for y in range(h)]
    dist = bevel(alpha, w, h)
    height = [[0.0] * w for _ in range(h)]
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            lum = (0.299 * r + 0.587 * g + 0.114 * b) / 255
            height[y][x] = (a / 255) * (0.5 * lum + 0.5 * dist[y][x] / BEVEL_PX)

    out = Image.new("RGBA", (w, h))
    op = out.load()
    for y in range(h):
        for x in range(w):
            if alpha[y][x] == 0:
                op[x, y] = (128, 128, 255, 255)
                continue
            hl = height[y][max(x - 1, 0)]
            hr = height[y][min(x + 1, w - 1)]
            hu = height[max(y - 1, 0)][x]  # fila de arriba en la imagen
            hd = height[min(y + 1, h - 1)][x]
            nx = -(hr - hl) * STRENGTH
            ny = (hd - hu) * STRENGTH  # filas crecen hacia abajo; Y del normal map crece hacia arriba
            nz = 1.0
            ln = math.sqrt(nx * nx + ny * ny + nz * nz)
            op[x, y] = tuple(int(round((c / ln * 0.5 + 0.5) * 255)) for c in (nx, ny, nz)) + (255,)
    return out


def main():
    count = 0
    for folder in FOLDERS:
        for src in sorted((ROOT / folder).rglob("*.png")):
            if src.stem.endswith("_n"):
                continue
            normal_map(src).save(src.with_name(src.stem + "_n.png"))
            count += 1
    print(f"{count} normal maps")


if __name__ == "__main__":
    main()
