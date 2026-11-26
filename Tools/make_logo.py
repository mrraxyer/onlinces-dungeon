"""Genera Assets/UI/Logo/logo.png (splash) con el título, las mascotas y "DEBUG DUNGEON".
Uso: python Tools/make_logo.py   (desde la raíz del proyecto)
"""
import re
from PIL import Image

A = "Assets/"
SCALE = 4  # px de arte -> px finales (nítido, sin filtro)
PURPLE = (124, 58, 237, 255)
GREEN = (74, 222, 128, 255)
INK = (24, 20, 37, 255)


def font_text(text, color):
    fnt = open("Assets/Fonts/pixel.fnt", encoding="utf8").read()
    sheet = Image.open("Assets/Fonts/pixel.png").convert("RGBA")
    chars = {int(m[0]): tuple(map(int, m[1:])) for m in
             re.findall(r"char id=(\d+) x=(\d+) y=(\d+) width=(\d+) height=(\d+) xoffset=(-?\d+) yoffset=(-?\d+) xadvance=(\d+)", fnt)}
    w = sum(chars[ord(c)][6] + 1 for c in text)
    out = Image.new("RGBA", (w, 12))
    x = 0
    for c in text:
        cx, cy, cw, ch, xo, yo, adv = chars[ord(c)]
        g = sheet.crop((cx, cy, cx + cw, cy + ch))
        solid = Image.new("RGBA", g.size, color)
        out.paste(solid, (x + xo, yo), g.split()[3].point(lambda a: 255 if a > 127 else 0))
        x += adv + 1
    return out.crop(out.getbbox())


def outlined(img, color, px=1):
    """Contorno de 1 px alrededor de lo opaco."""
    w, h = img.size
    out = Image.new("RGBA", (w + 2 * px, h + 2 * px))
    mask = img.split()[3]
    solid = Image.new("RGBA", img.size, color)
    for dx in range(-px, px + 1):
        for dy in range(-px, px + 1):
            out.paste(solid, (px + dx, px + dy), mask)
    out.paste(img, (px, px), img)
    return out


def main():
    title = Image.open(A + "UI/Sprites/title.png").convert("RGBA")
    on = Image.open(A + "Characters/Sprites/On/on_full.png").convert("RGBA").crop((0, 0, 48, 48))
    off = Image.open(A + "Characters/Sprites/Off/off_full.png").convert("RGBA").crop((0, 0, 48, 48))
    on, off = on.crop(on.getbbox()), off.crop(off.getbbox())

    word = outlined(font_text("DEBUG DUNGEON", GREEN), INK)
    word = word.resize((word.width * 2, word.height * 2), Image.NEAREST)
    bar_w = word.width + 16

    gap = 6
    w = on.width + gap + title.width + gap + off.width
    h = max(on.height, title.height + 6 + word.height)
    logo = Image.new("RGBA", (w + 4, h + 4))
    y_title = (h - (title.height + 6 + word.height)) // 2 + 2
    logo.paste(on, (2, h - on.height + 2), on)
    logo.paste(off, (2 + on.width + gap + title.width + gap, h - off.height + 2), off)
    tx = 2 + on.width + gap
    logo.paste(title, (tx, y_title), title)
    wx = tx + (title.width - word.width) // 2
    wy = y_title + title.height + 6
    # barra morada detrás del subtítulo
    bar = Image.new("RGBA", (bar_w, word.height - 4), PURPLE)
    logo.paste(bar, (tx + (title.width - bar_w) // 2, wy + 2))
    logo.paste(word, (wx, wy), word)

    logo = logo.resize((logo.width * SCALE, logo.height * SCALE), Image.NEAREST)
    logo.save("Assets/UI/Logo/logo.png")
    assert logo.getbbox() is not None and logo.width > 600, logo.size
    print("logo", logo.size)


if __name__ == "__main__":
    main()
